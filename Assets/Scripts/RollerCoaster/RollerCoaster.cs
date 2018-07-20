﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RollerCoaster : MonoBehaviour {

    List<GameObject> trackPieces = new List<GameObject>();

    //List containing disabled track pieces. This is used because creating and destroying gameobjects constantly causes massive amounts of lag.
    List<GameObject> unusedTrackPieces = new List<GameObject>();

    //the scale the world is set at
    public static float scale = 0.008f;

    //the length of one track's bone
    public float trackBoneSize = 0.402642f;

    //the prefab for an empty piece of track
    public GameObject trackPrefab;

    //just for now, the right controller is going to be loaded in here
    public GameObject rightController;

    //the latest track to be permenently placed
    public GameObject currentTrack;

    //is the incline being edited or the turns. true for incline, false for turns
    public bool inclineMode = false;

    void Start () {
        //just for now, since we must start with one
        transform.Find("TrackPiece0").gameObject.GetComponent<TrackPiece>().rollerCoaster = this;
        trackPieces.Add(transform.Find("TrackPiece0").gameObject);

        //TODO: set tracksize dynamically based on calling the TrackPiece class

        //set track bone size based on scale
        trackBoneSize *= scale;

        currentTrack = trackPieces[0];
    }
	
	void Update () {

        GameController gameController = GameController.instance;

        //check if the straight button is being held down

        CreatePath(currentTrack, inclineMode);

        if (Input.GetButtonDown("RightTrackpadClick")) {
            inclineMode = !inclineMode;
        } else if (Input.GetAxis("RightTrigger") > 0.5 || Input.anyKeyDown) {
            currentTrack = trackPieces[trackPieces.Count - 1];
        }

    }

    //will create a path of tracks from a start position until the next position by creating turns between them
    //startTrack: track that this path is starting on
    //incline: if this path is creating an incline, false if it is creating a turning path
    public void CreatePath(GameObject startTrack, bool incline) {
        //if any tracks should be created
        bool cancel = false;

        bool straight = Input.GetButton("RightMenuClick");

        //the position of the first track piece that will be a part of this new edition (previous track pieces are not edited)
        Vector3 startPosition = startTrack.transform.position;
        Vector3 targetPosition = rightController.transform.position;

        Vector3 targetAngle = new Vector3(0, 1, 0) * rightController.transform.eulerAngles.y;
        Vector3 fullTargetAngle = rightController.transform.eulerAngles;
        Vector3 startTrackAngleRelative = Vector3.zero;
        Vector3 currentAngle = getCurrentAngle(startTrack, true);
        //full angle without any edits
        Vector3 fullStartAngle = getCurrentAngle(startTrack, true);
        if (incline) {
            targetAngle = new Vector3(1, 0, 0) * rightController.transform.eulerAngles.x;
            currentAngle = getCurrentAngle(startTrack, false);
            currentAngle = new Vector3(currentAngle.x, currentAngle.z, currentAngle.y);
            fullStartAngle = getCurrentAngle(startTrack, false);
        }

        if (incline) {
            //adjust angle to make it like it was normal
            float angle = Mathf.Cos(fullStartAngle.z * Mathf.Deg2Rad) * fullTargetAngle.x + Mathf.Sin(fullStartAngle.z * Mathf.Deg2Rad + Mathf.PI) * fullTargetAngle.z;

            targetAngle = new Vector3(angle, 0, 0);

            //theoretical position as if it was at a normal position
            float x = Mathf.Sin(fullStartAngle.z * Mathf.Deg2Rad) * targetPosition.x + Mathf.Cos(fullStartAngle.z * Mathf.Deg2Rad) * targetPosition.z;
            float y = targetPosition.y;

            //set that position so that future calculations use that position instead
            targetPosition = new Vector3(0, y, x);
        }
        //rotate positions around the start angle
        Vector3 pivotAngle = -currentAngle;
        if (incline) {
            pivotAngle = - new Vector3(1, 0, 0) * currentAngle.x;
            pivotAngle += new Vector3(90, 0, 0);
        }
        targetPosition = RotatePointAroundPivot(targetPosition, startPosition, pivotAngle);
        if (!incline) {
            targetAngle -= new Vector3(0, 1, 0) * currentAngle.y;
        } else {
            targetAngle -= new Vector3(1, 0, 0) * currentAngle.x;
        }

        //set angle to 0 if the straight button is being held
        if (straight) {
            targetAngle = Vector3.zero;
            fullTargetAngle = Vector3.zero;
        }

        Vector3 angleDifference = targetAngle - startTrackAngleRelative;
        //make sure the smallest difference between the angles is found
        Vector3 smallestAngleDifference = new Vector3(Mathf.Abs(angleDifference.x), Mathf.Abs(angleDifference.y), Mathf.Abs(angleDifference.z));
        //do 360 - angle if over 180 for each (see https://stackoverflow.com/questions/6722272/smallest-difference-between-two-angles)
        {
            float x1 = smallestAngleDifference.x;
            float y1 = smallestAngleDifference.y;
            float z1 = smallestAngleDifference.z;

            if (x1 > 180) {
                x1 = 360 - x1;
            }

            if (y1 > 180) {
                y1 = 360 - y1;
            }

            if (z1 > 180) {
                z1 = 360 - z1;
            }

            smallestAngleDifference = new Vector3(x1, y1, z1);
        }

        //get amount of tracks needed by dividing by length of one track's bone then dividing by amount of bones per track piece
        //int for now just to make things easier
        //for now just set to a static number

        //that many tracks can now be created with an angle of angle.y divided by each bone (tracksNeeded * 10f)

        //find the collision between the start line and the target line (x = (b2 - b1) / (m1 - m2))

        //calculate the slope for the target angle
        float targetSlopeAngle = 90 - targetAngle.y;
        if (incline) {
            targetSlopeAngle = 90 - targetAngle.x;
        }
        float targetSlope = Mathf.Tan(targetSlopeAngle * Mathf.Deg2Rad);
        //calculate slope for the start
        float startSlopeAngle = 90 - startTrackAngleRelative.y;
        if (incline) {
            startSlopeAngle = 90 - startTrackAngleRelative.x;
        }
        float startSlope = Mathf.Tan(startSlopeAngle * Mathf.Deg2Rad);

        //the b value for the target angle (b = y - mx)
        float targetB = targetPosition.z - targetSlope * targetPosition.x;
        //the b value for the start angle (b = y - mx)
        float startB = startPosition.z - startSlope * startPosition.x;
        if (incline) {
            targetB = targetPosition.y - targetSlope * targetPosition.z;
            startB = startPosition.y - startSlope * startPosition.z;
        }

        //calculate the collision point
        float collisionX = (startB - targetB) / (targetSlope - startSlope);
        float collisionY = targetSlope * collisionX + targetB;

        if (!incline) {
            //check if that point is infront or behind the start point (https://math.stackexchange.com/questions/1330210/how-to-check-if-a-point-is-in-the-direction-of-the-normal-of-a-plane)
            Vector3 startNormal = new Vector3(Mathf.Cos(startSlopeAngle * Mathf.Deg2Rad), 0, Mathf.Sin(startSlopeAngle * Mathf.Deg2Rad));
            float startNormalDistance = Vector3.Dot(startNormal, new Vector3(collisionX, 0, collisionY) - startPosition);
            //check if the collision point is past the startPosition
            if (startNormalDistance > 0 && targetAngle.y != 0) {
                cancel = true;
            }
            //check for the target line as well
            Vector3 targetNormal = new Vector3(Mathf.Cos(targetSlopeAngle * Mathf.Deg2Rad), 0, Mathf.Sin(targetSlopeAngle * Mathf.Deg2Rad));
            float targetNormalDistance = Vector3.Dot(targetNormal, new Vector3(collisionX, 0, collisionY) - targetPosition);
            //check if the collision point is past the startPosition
            if (targetNormalDistance < 0 && targetAngle.y != 0) {
                cancel = true;
            }
        } else if (incline) {
            //check if that point is infront or behind the start point (https://math.stackexchange.com/questions/1330210/how-to-check-if-a-point-is-in-the-direction-of-the-normal-of-a-plane)
            Vector3 startNormal = new Vector3(0, Mathf.Sin(startSlopeAngle * Mathf.Deg2Rad), Mathf.Cos(startSlopeAngle * Mathf.Deg2Rad));
            float startNormalDistance = Vector3.Dot(startNormal, new Vector3(0, collisionY, collisionX) - startPosition);
            //check if the collision point is past the startPosition
            if (startNormalDistance < 0 && targetAngle.x != 0) {
                cancel = true;
            }
            //check for the target line as well
            Vector3 targetNormal = new Vector3(0, Mathf.Sin(targetSlopeAngle * Mathf.Deg2Rad), Mathf.Cos(targetSlopeAngle * Mathf.Deg2Rad));
            float targetNormalDistance = Vector3.Dot(targetNormal, new Vector3(0, collisionY, collisionX) - targetPosition);
            //check if the collision point is past the startPosition
            if (targetNormalDistance > 0 && targetAngle.x != 0) {
                cancel = true;
            }
        }

        //get distance from the start
        float distanceFromStart = Mathf.Sqrt(Mathf.Pow(collisionX - startPosition.x, 2)
            + Mathf.Pow(collisionY - startPosition.z, 2));

        //get distance from target
        float distanceFromTarget = Mathf.Sqrt(Mathf.Pow(collisionX - targetPosition.x, 2)
            + Mathf.Pow(collisionY - targetPosition.z, 2));

        if (incline) {
            distanceFromStart = Mathf.Sqrt(Mathf.Pow(collisionX - startPosition.z, 2)
            + Mathf.Pow(collisionY - startPosition.y, 2));

            distanceFromTarget = Mathf.Sqrt(Mathf.Pow(collisionX - targetPosition.z, 2)
                + Mathf.Pow(collisionY - targetPosition.y, 2));
        }

        //if the angle is 0, then get the normal difference, do not try to form a curve
        if ((targetAngle.x == 0 && !incline) || (targetAngle.y == 0 && incline)) {
            distanceFromStart = Vector3.Distance(startPosition, targetPosition);
        }

        //float trackLengthRequired = 2 * Mathf.PI * radius * ((180 - angle.y) / 360);

        //get amount of tracks needed by dividing by length of one track's bone then dividing by amount of bones per track piece
        //int for now just to make things easier

        //the amount of tracks need coming straight off the start track
        float startTracksNeeded = Mathf.Abs(distanceFromStart / (trackBoneSize * 10f));
        float targetTracksNeeded = Mathf.Abs(distanceFromTarget / (trackBoneSize * 10f));
        float curveTracksNeeded = 0;

        //if the controller is on the other side
        bool otherSide = targetPosition.x > startPosition.x;
        if (incline) {
            otherSide = targetPosition.z < startPosition.z;
        }

        //amount to check for for the first if statement in the curve
        float checkAmount = startTracksNeeded;

        if (Mathf.Min(startTracksNeeded, targetTracksNeeded) == checkAmount && !cancel) {
            //find intersection between line to the end of curve from the start of curve
            float startToEndCurveSlope = Mathf.Tan((((180 - targetAngle.y) / 2)) * Mathf.Deg2Rad);
            //the b value (b = y - mx)
            float startToEndCurveB = startPosition.z - startToEndCurveSlope * startPosition.x;

            if (incline) {
                startToEndCurveSlope = Mathf.Tan((((180 - targetAngle.x) / 2)) * Mathf.Deg2Rad);
                startToEndCurveB = startPosition.y - startToEndCurveSlope * startPosition.z;
            }

            //find intersection between this line and the target line (x = (b2 - b1) / (m1 - m2))
            //this position will be the second point on the circle of the curve (end point), the first is the start track
            float circleTargetX = (startToEndCurveB - targetB) / (targetSlope - startToEndCurveSlope);
            float circleTargetY = startToEndCurveSlope * circleTargetX + startToEndCurveB;

            //find the normal for the start angle

            //y = rsinA, x = rcosA
            //these are the positions of these angles on a circle with a radius of 1
            float targetNormalX = Mathf.Cos((-targetAngle.y + 360) * Mathf.Deg2Rad);
            float targetNormalY = Mathf.Sin((-targetAngle.y + 360) * Mathf.Deg2Rad);
            float startNormalX = Mathf.Cos(startTrackAngleRelative.y * Mathf.Deg2Rad);
            float startNormalY = Mathf.Sin(startTrackAngleRelative.y * Mathf.Deg2Rad);

            if (incline) {
                targetNormalX = Mathf.Cos((-targetAngle.x + 360) * Mathf.Deg2Rad);
                targetNormalY = Mathf.Sin((-targetAngle.x + 360) * Mathf.Deg2Rad);
                startNormalX = Mathf.Cos(startTrackAngleRelative.x * Mathf.Deg2Rad);
                startNormalY = Mathf.Sin(startTrackAngleRelative.x * Mathf.Deg2Rad);
            }

            //the radius would be equal to 1 for a circle like this. Find how much the distances between the points account for the radius of the circle
            float percentageOfRadius = Mathf.Sqrt(Mathf.Pow(startNormalX - targetNormalX, 2) + Mathf.Pow(startNormalY - targetNormalY, 2));

            //radius of the curve using the percentage calculations from above
            float radius = Mathf.Sqrt(Mathf.Pow(circleTargetX - startPosition.x, 2) + Mathf.Pow(circleTargetY - startPosition.z, 2)) / percentageOfRadius;

            //calculate the cirumference of this circle multiplied by the amount this curve takes up of the whole circle
            float curveLength = 2 * Mathf.PI * radius * (smallestAngleDifference.y / 360f);

            if (incline) {
                radius = Mathf.Sqrt(Mathf.Pow(circleTargetX - startPosition.z, 2) + Mathf.Pow(circleTargetY - startPosition.y, 2)) / percentageOfRadius;
                curveLength = 2 * Mathf.PI * radius * (smallestAngleDifference.x / 360f);
            }

            curveTracksNeeded = (curveLength / (trackBoneSize * 10f));

            //curve too small
            if (curveTracksNeeded < 1) {
                cancel = true;
            }

            startTracksNeeded = 0;

            //Find difference between circleTarget and the target position
            targetTracksNeeded = (Mathf.Sqrt(Mathf.Pow(circleTargetX - targetPosition.x, 2) + Mathf.Pow(circleTargetY - targetPosition.z, 2)) / (trackBoneSize * 10f));

            if (incline) {
                targetTracksNeeded = (Mathf.Sqrt(Mathf.Pow(circleTargetX - targetPosition.z, 2) + Mathf.Pow(circleTargetY - targetPosition.y, 2)) / (trackBoneSize * 10f));
            }

            if ((targetAngle.y == 0 && !incline) || (targetAngle.x == 0 && incline)) {
                targetTracksNeeded = distanceFromStart / (trackBoneSize * 10f);
                curveTracksNeeded = 0;
            }

        } else if (!cancel) {
            //find intersection between line to the start of curve from the end of curve
            float endToStartCurveSlope = Mathf.Tan((((180 - targetAngle.y) / 2)) * Mathf.Deg2Rad);
            //the b value (b = y - mx)
            float endToStartCurveB = targetPosition.z - endToStartCurveSlope * targetPosition.x;

            if (incline) {
                endToStartCurveSlope = Mathf.Tan((((180 - targetAngle.x) / 2)) * Mathf.Deg2Rad);
                endToStartCurveB = targetPosition.y - endToStartCurveSlope * targetPosition.z;
            }

            //find intersection between this line and the start line (x = (b2 - b1) / (m1 - m2))
            //this position will be the second point on the circle of the curve (end point), the first is the target track
            float circleStartX = (endToStartCurveB - startB) / (startSlope - endToStartCurveSlope);
            float circleStartY = endToStartCurveSlope * circleStartX + endToStartCurveB;

            //y = rsinA, x = rcosA
            //these are the positions of these angles on a circle with a radius of 1
            float targetNormalX = Mathf.Cos((-targetAngle.y + 360) * Mathf.Deg2Rad);
            float targetNormalY = Mathf.Sin((-targetAngle.y + 360) * Mathf.Deg2Rad);
            float startNormalX = Mathf.Cos(startTrackAngleRelative.y * Mathf.Deg2Rad);
            float startNormalY = Mathf.Sin(startTrackAngleRelative.y * Mathf.Deg2Rad);

            if (incline) {
                targetNormalX = Mathf.Cos((-targetAngle.x + 360) * Mathf.Deg2Rad);
                targetNormalY = Mathf.Sin((-targetAngle.x + 360) * Mathf.Deg2Rad);
                startNormalX = Mathf.Cos(startTrackAngleRelative.x * Mathf.Deg2Rad);
                startNormalY = Mathf.Sin(startTrackAngleRelative.x * Mathf.Deg2Rad);
            }

            //the radius would be equal to 1 for a circle like this. Find how much the distances between the points account for the radius of the circle
            float percentageOfRadius = Mathf.Sqrt(Mathf.Pow(startNormalX - targetNormalX, 2) + Mathf.Pow(startNormalY - targetNormalY, 2));

            //radius of the curve using the percentage calculations from above
            float radius = Mathf.Sqrt(Mathf.Pow(circleStartX - targetPosition.x, 2) + Mathf.Pow(circleStartY - targetPosition.z, 2)) / percentageOfRadius;

            //calculate the cirumference of this circle multiplied by the amount this curve takes up of the whole circle
            float curveLength = 2 * Mathf.PI * radius * (smallestAngleDifference.y / 360f);

            if (incline) {
                radius = Mathf.Sqrt(Mathf.Pow(circleStartX - targetPosition.z, 2) + Mathf.Pow(circleStartY - targetPosition.y, 2)) / percentageOfRadius;
                curveLength = 2 * Mathf.PI * radius * (smallestAngleDifference.x / 360f);
            }

            curveTracksNeeded = (curveLength / (trackBoneSize * 10f));

            //curve too small
            if (curveTracksNeeded < 1) {
                cancel = true;
            }

            //Find difference between circleTarget and the target position
            startTracksNeeded = (Mathf.Sqrt(Mathf.Pow(circleStartX - startPosition.x, 2) + Mathf.Pow(circleStartY - startPosition.z, 2)) / (trackBoneSize * 10f));

            if (incline) {
                startTracksNeeded = (Mathf.Sqrt(Mathf.Pow(circleStartX - startPosition.z, 2) + Mathf.Pow(circleStartY - startPosition.y, 2)) / (trackBoneSize * 10f));
            }

            targetTracksNeeded = 0;
        }

        Func<int> totalTracksNeeded = () => Mathf.CeilToInt(startTracksNeeded) + Mathf.CeilToInt(curveTracksNeeded) + Mathf.CeilToInt(targetTracksNeeded);

        if (otherSide) {

            if (!incline) {
                smallestAngleDifference = new Vector3(smallestAngleDifference.x, -smallestAngleDifference.y, smallestAngleDifference.z);
            } else if (incline) {
                smallestAngleDifference = new Vector3(-smallestAngleDifference.x, smallestAngleDifference.y, smallestAngleDifference.z);

            }
        }

        //check if the generation was canceled or the track is too big or small
        if (cancel || totalTracksNeeded() > 250 || (curveTracksNeeded < 3 && targetAngle != Vector3.zero)) {
            totalTracksNeeded = () => 0;
        }

        //angle to start from when curves start if part of the curve is drawn during the start tracks
        Vector3 startTrackAngle = Vector3.zero;

        //has the first piece been edited already
        bool firstPieceEdited = false;

        //Amount of tracks already placed down
        int startTrackAmount = trackPieces.IndexOf(startTrack) + 1;
        for (int i = 0; i < totalTracksNeeded(); i++) {
            //should the i not increment at the end
            bool reset = false;

            Vector3 eulerAngles = startTrackAngleRelative;
            eulerAngles += currentAngle;

            //the total angle going through one whole track piece
            Vector3 totalTrackAngle = Vector3.zero;
            //angle that the tracks start with if they have a startTrack value that is not -1
            Vector3 startAngle = Vector3.zero;

            GameObject premadeTrackPiece = null;
            //should a premade track piece be used instead of creating a new one
            bool usePremadeTrackPiece = false;

            float percentageOfTrack = 1;

            if (i < Mathf.CeilToInt(startTracksNeeded)) {
                //set it to the part of the track nessesary to finish drawing the startTracksNeeded
                percentageOfTrack = 1;
                if (startTracksNeeded - i < 1) {
                    percentageOfTrack = startTracksNeeded - i;

                    int curveStartNum = (int)((1 - percentageOfTrack) * 10f);

                    totalTrackAngle = (smallestAngleDifference / (curveTracksNeeded * 10f)) * curveStartNum;
                    startAngle = Vector3.zero;

                    startTrackAngle = totalTrackAngle;
                    smallestAngleDifference -= startTrackAngle;

                    //the remaining part of the track can be used to start the curve
                    curveTracksNeeded -= curveStartNum / 10f;
                }
            }

            if (i >= Mathf.CeilToInt(startTracksNeeded) && i < Mathf.CeilToInt(startTracksNeeded) + Mathf.CeilToInt(curveTracksNeeded)) {
                //then it is time to create a curve instead of just a straight line coming off the start track
                //calculate the adjustment needed for the curve
                eulerAngles = smallestAngleDifference / curveTracksNeeded * (i - Mathf.CeilToInt(startTracksNeeded)) + startTrackAngleRelative;
                eulerAngles += startTrackAngle;
                eulerAngles += currentAngle;

                totalTrackAngle = smallestAngleDifference / curveTracksNeeded;

                //set it to the part of the track nessesary to finish drawing the curveTracksNeeded
                percentageOfTrack = 1;
                if (curveTracksNeeded - (i - Mathf.CeilToInt(startTracksNeeded)) < 1) {
                    percentageOfTrack = curveTracksNeeded - (i - Mathf.CeilToInt(startTracksNeeded));

                    //this means there are more tracks after the curve, part of the curve track can be used for that
                    if (targetTracksNeeded > 0) {
                        int curveStartNum = (int)((1 - percentageOfTrack) * 10f);
                        int curveEndNum = (int)((percentageOfTrack) * 10f);

                        startAngle = (smallestAngleDifference / (curveTracksNeeded * 10f)) * curveEndNum;

                        totalTrackAngle = Vector3.zero;

                        //the remaining part of the track can be used for the target tracks needed
                        targetTracksNeeded -= curveStartNum / 10f;
                    }
                }
            }

            if (i >= Mathf.CeilToInt(startTracksNeeded) + Mathf.CeilToInt(curveTracksNeeded)) {
                //back to straight path, but in the angle of the target
                eulerAngles = targetAngle;
                eulerAngles += currentAngle;
                totalTrackAngle = Vector3.zero;

                //set it to the part of the track nessesary to finish drawing the targetTracksNeeded
                percentageOfTrack = 1;
                if (targetTracksNeeded - (i - Mathf.CeilToInt(startTracksNeeded) - Mathf.CeilToInt(curveTracksNeeded)) < 1) {
                    percentageOfTrack = targetTracksNeeded - (i - Mathf.CeilToInt(startTracksNeeded) - Mathf.CeilToInt(curveTracksNeeded));
                }
            }

            int secondCurveStart = -1;
            if (percentageOfTrack < 1 && i < Mathf.CeilToInt(startTracksNeeded)) {
                //the remaining track will be used for the curve
                secondCurveStart = (int)((percentageOfTrack) * 10f);

            }
            if (percentageOfTrack < 1 && i >= Mathf.CeilToInt(startTracksNeeded) && i < Mathf.CeilToInt(startTracksNeeded) + Mathf.CeilToInt(curveTracksNeeded) && targetTracksNeeded > 0) {
                //the remaining track will be used for the curve
                secondCurveStart = (int)((percentageOfTrack) * 10f);
            }

            //check to see if this can merge with the start track
            if (!firstPieceEdited && i == 0 && ((startTrack.GetComponent<TrackPiece>().percentageOfTrack != 1 && startTrack.GetComponent<TrackPiece>().secondCurveStart == -1) || startTrack.GetComponent<TrackPiece>().modified)) {
                if (startTracksNeeded > 0) {
                    percentageOfTrack = startTrack.GetComponent<TrackPiece>().percentageOfTrack;

                    int curveStartNum = (int)((1 - percentageOfTrack) * 10f);

                    TrackPiece startTrackScript = startTrack.GetComponent<TrackPiece>();

                    startAngle = startTrackScript.totalAngle;
                    if (startTrackScript.modified) {
                        startAngle = startTrackScript.oldTotalAngle;
                    } else {
                        startTrackScript.oldTotalAngle = startTrackScript.totalAngle;
                    }

                    totalTrackAngle = Vector3.zero;

                    //remove this amount as it was already dealt with here
                    startTracksNeeded -= curveStartNum / 10f;

                    secondCurveStart = (int)((percentageOfTrack) * 10f);

                    reset = true;
                    firstPieceEdited = true;

                    usePremadeTrackPiece = true;
                    premadeTrackPiece = startTrack;
                } else {
                    percentageOfTrack = startTrack.GetComponent<TrackPiece>().percentageOfTrack;

                    int curveStartNum = (int)((1 - percentageOfTrack) * 10f);
                    int curveEndNum = (int)((percentageOfTrack) * 10f);

                    TrackPiece startTrackScript = startTrack.GetComponent<TrackPiece>();

                    startAngle = startTrackScript.totalAngle;
                    if (startTrackScript.modified) {
                        startAngle = startTrackScript.oldTotalAngle;
                    } else {
                        startTrackScript.oldTotalAngle = startTrackScript.totalAngle;
                    }

                    totalTrackAngle = (smallestAngleDifference / (curveTracksNeeded * 10f)) * curveStartNum;

                    secondCurveStart = (int)((percentageOfTrack) * 10f);

                    //subtrack by the amount not done
                    startTrackAngle = totalTrackAngle;
                    smallestAngleDifference -= startTrackAngle;

                    //remove this amount as it was already dealt with here
                    curveTracksNeeded -= curveStartNum / 10f;

                    reset = true;
                    firstPieceEdited = true;

                    usePremadeTrackPiece = true;
                    premadeTrackPiece = startTrack;
                }
            }

            if (startTrackAmount + i < trackPieces.Count || usePremadeTrackPiece) {
                GameObject trackPiece = null;

                if (usePremadeTrackPiece) {
                    trackPiece = premadeTrackPiece;
                    premadeTrackPiece.GetComponent<TrackPiece>().modified = true;
                } else {
                    trackPiece = trackPieces[i + startTrackAmount];
                }

                Vector3 oldPosition = trackPiece.transform.position;
                Vector3 oldAngles = trackPiece.transform.eulerAngles;

                //reset position and angle before adjusting the track
                trackPiece.transform.position = Vector3.zero;
                trackPiece.transform.localEulerAngles = Vector3.zero;

                //adjust the track
                trackPiece.GetComponent<TrackPiece>().AdjustTrack(totalTrackAngle, startAngle, percentageOfTrack, secondCurveStart);

                //premade track piece would already be in the correct position
                if (!usePremadeTrackPiece) {
                    //calculate adjustments
                    //this finds the last bone plus half of the track size (because position is based off the center of the object
                    Vector3 modifiedPosition = trackPieces[i + startTrackAmount - 1].transform.Find("Bottom_Rail/Joint_3_3/Joint_1_3/Joint_2_4/Joint_3_4/Joint_4_3/Joint_5_3/Joint_6_3/Joint_7_3/Joint_8_3/Joint_9_3/Joint_10_3").position;

                    //need to offset it by trackBoneSize by the angle
                    Vector3 offset = (new Vector3(Mathf.Sin(eulerAngles.y * Mathf.Deg2Rad), 0, Mathf.Cos(eulerAngles.y * Mathf.Deg2Rad)) * (trackBoneSize * 5));
                    if (incline || eulerAngles.x != 0 || eulerAngles.y != 0) {
                        offset = (new Vector3(0, -Mathf.Sin(eulerAngles.x * Mathf.Deg2Rad), Mathf.Cos(eulerAngles.x * Mathf.Deg2Rad)) * (trackBoneSize * 5));

                        //rotate the offset by the y angle incase it needs to be pointing in a different direction
                        offset = RotatePointAroundPivot(offset, Vector3.zero, new Vector3(0, 1, 0) * eulerAngles.y);
                    }

                    //subtract offset
                    trackPiece.transform.position = modifiedPosition - offset;

                    //set track rotation (after adjustment to make sure the adjustment process goes well)
                    trackPiece.transform.localEulerAngles = eulerAngles;
                } else {
                    //set it to what it was before
                    trackPiece.transform.position = oldPosition;
                    trackPiece.transform.localEulerAngles = oldAngles;
                }

            } else {
                //calculate adjustments
                //this finds the last bone plus half of the track size (because position is based off the center of the object
                Vector3 modifiedPosition = trackPieces[i + startTrackAmount - 1].transform.Find("Bottom_Rail/Joint_3_3/Joint_1_3/Joint_2_4/Joint_3_4/Joint_4_3/Joint_5_3/Joint_6_3/Joint_7_3/Joint_8_3/Joint_9_3/Joint_10_3").position;

                //need to offset it by trackBoneSize by the angle
                Vector3 offset = (new Vector3(Mathf.Sin(eulerAngles.y * Mathf.Deg2Rad), 0, Mathf.Cos(eulerAngles.y * Mathf.Deg2Rad)) * (trackBoneSize * 5));
                if (incline) {
                    offset = (new Vector3(0, -Mathf.Sin(eulerAngles.x * Mathf.Deg2Rad), Mathf.Cos(eulerAngles.x * Mathf.Deg2Rad)) * (trackBoneSize * 5));
                }

                GameObject trackPiece = AddTrackPiece(totalTrackAngle, modifiedPosition, eulerAngles, startAngle, offset, percentageOfTrack, secondCurveStart);
            }

            //reset i if nessesary (this must be the last thing in the loop)
            if (reset) {
                i--;
            }
        }

        //remove all unneeded track pieces, don't add to i since trackPieces.Count will be continuing to shrink
        for (int i = Mathf.CeilToInt(startTrackAmount + totalTracksNeeded()); i < trackPieces.Count;) {
            RemoveTrackPiece(trackPieces[i]);
        }
        
        //reset last track back to normal if nessesary
        if (totalTracksNeeded() == 0 && startTrack.GetComponent<TrackPiece>().modified) {
            TrackPiece trackPiece = startTrack.GetComponent<TrackPiece>();

            Vector3 oldPosition = trackPiece.transform.position;
            Vector3 oldAngles = trackPiece.transform.eulerAngles;

            //reset position and angle before adjusting the track
            trackPiece.transform.position = Vector3.zero;
            trackPiece.transform.localEulerAngles = Vector3.zero;

            //adjust the track back the how it was
            trackPiece.AdjustTrack(trackPiece.oldTotalAngle, Vector3.zero, trackPiece.percentageOfTrack, -1);

            //set it to what it was before
            trackPiece.transform.position = oldPosition;
            trackPiece.transform.localEulerAngles = oldAngles;

            startTrack.GetComponent<TrackPiece>().modified = false;
        }

    }

    public GameObject AddTrackPiece (Vector3 totalAngle, Vector3 modifiedPosition, Vector3 eulerAngles, Vector3 startAngle, Vector3 offset, float percentageOfTrack, int secondCurveStart) {
        GameObject newTrackPiece;

        if(unusedTrackPieces.Count > 0) {
            newTrackPiece = unusedTrackPieces[0];
            unusedTrackPieces.RemoveAt(0);

            newTrackPiece.SetActive(true);
        } else {
            newTrackPiece = Instantiate(trackPrefab, transform);

            TrackPiece trackPieceClass = newTrackPiece.GetComponent<TrackPiece>();

            trackPieceClass.Start();
            trackPieceClass.rollerCoaster = this;
        }

        //reset position and angle before adjusting the track
        newTrackPiece.transform.position = Vector3.zero;
        newTrackPiece.transform.localEulerAngles = Vector3.zero;

        TrackPiece newTrackPieceClass = newTrackPiece.GetComponent<TrackPiece>();

        newTrackPieceClass.totalAngle = totalAngle;
        trackPieces.Add(newTrackPiece);

        //adjust the track
        newTrackPieceClass.AdjustTrack(totalAngle, startAngle, percentageOfTrack, secondCurveStart);

        //set track rotation (after adjustment to make sure the adjustment process goes well)
        newTrackPiece.transform.eulerAngles = eulerAngles;
        //subtract offset
        newTrackPiece.transform.position = modifiedPosition - offset;

        return newTrackPiece;
    }

    public void RemoveTrackPiece(GameObject trackPiece) {
        unusedTrackPieces.Add(trackPiece);
        trackPieces.Remove(trackPiece);

        trackPiece.SetActive(false);
    }

    //normal: should it use the normal start track rotation or use a converted version using MathHelper.ConvertQuant2Euler()
    public Vector3 getCurrentAngle(GameObject startTrack, bool normal) {
        Vector3 currentAngle = Vector3.zero;

        TrackPiece trackPiece = startTrack.GetComponent<TrackPiece>();

        if (!trackPiece.modified) {
            currentAngle = trackPiece.totalAngle;
        }
        currentAngle += trackPiece.startAngle;

        //that function does not puts the angle into the x, so it is not useful when doing turns
        if (normal) {
            currentAngle += startTrack.transform.localEulerAngles;
        } else {
            currentAngle += MathHelper.ConvertQuant2Euler(startTrack.gameObject.transform.rotation);
        }

        return currentAngle;
    }

    //from https://answers.unity.com/questions/532297/rotate-a-vector-around-a-certain-point.html
    public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angle) {
        Vector3 dir = point - pivot; //get point direction relative to pivot

        dir = Quaternion.Euler(angle) * dir; //rotate it
        point = dir + pivot; //calculate rotated point

        return point;
    }


}
