// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
 
namespace SVGImporter.Utils
{
    public class SVGMath {
     
        public static Vector2 RotateVectorClockwise(Vector2 vector)
        {
            return new Vector2(vector.y, -vector.x);
        }

        public static Vector2 RotateVectorAntiClockwise(Vector2 vector)
        {
            return new Vector2(-vector.y, vector.x);
        }

        public static int PositiveModulo(int a, int b)
        {
            return (Mathf.Abs(a * b) + a) % b;
        }

    	//increase or decrease the length of vector by size
    	public static Vector3 AddVectorLength(Vector3 vector, float size){
     
    		//get the vector length
    		float magnitude = Vector3.Magnitude(vector);
     
    		//change the length
    		magnitude += size;
     
    		//normalize the vector
    		Vector3 vectorNormalized = Vector3.Normalize(vector);
     
    		//scale the vector
    		return Vector3.Scale(vectorNormalized, new Vector3(magnitude, magnitude, magnitude));		
    	}
     
    	//create a vector of direction "vector" with length "size"
    	public static Vector3 SetVectorLength(Vector3 vector, float size){
     
    		//normalize the vector
    		Vector3 vectorNormalized = Vector3.Normalize(vector);
     
    		//scale the vector
    		return vectorNormalized *= size;
    	}
     
     
    	//caclulate the rotational difference from A to B
    	public static Quaternion SubtractRotation(Quaternion B, Quaternion A){
     
    		Quaternion C = Quaternion.Inverse(A) * B;		
    		return C;
    	}
     
    	//Find the line of intersection between two planes.	The planes are defined by a normal and a point on that plane.
    	//The outputs are a point on the line and a vector which indicates it's direction. If the planes are not parallel, 
    	//the function outputs true, otherwise false.
    	public static bool PlanePlaneIntersection(out Vector3 linePoint, out Vector3 lineVec, Vector3 plane1Normal, Vector3 plane1Position, Vector3 plane2Normal, Vector3 plane2Position){
     
    		linePoint = Vector3.zero;
    		lineVec = Vector3.zero;
     
    		//We can get the direction of the line of intersection of the two planes by calculating the 
    		//cross product of the normals of the two planes. Note that this is just a direction and the line
    		//is not fixed in space yet. We need a point for that to go with the line vector.
    		lineVec = Vector3.Cross(plane1Normal, plane2Normal);
     
    		//Next is to calculate a point on the line to fix it's position in space. This is done by finding a vector from
    		//the plane2 location, moving parallel to it's plane, and intersecting plane1. To prevent rounding
    		//errors, this vector also has to be perpendicular to lineDirection. To get this vector, calculate
    		//the cross product of the normal of plane2 and the lineDirection.		
    		Vector3 ldir = Vector3.Cross(plane2Normal, lineVec);		
     
    		float denominator = Vector3.Dot(plane1Normal, ldir);
     
    		//Prevent divide by zero and rounding errors by requiring about 5 degrees angle between the planes.
    		if(Mathf.Abs(denominator) > 0.006f){
     
    			Vector3 plane1ToPlane2 = plane1Position - plane2Position;
    			float t = Vector3.Dot(plane1Normal, plane1ToPlane2) / denominator;
    			linePoint = plane2Position + t * ldir;
     
    			return true;
    		}
     
    		//output not valid
    		else{
    			return false;
    		}
    	}	
     
    	//Get the intersection between a line and a plane. 
    	//If the line and plane are not parallel, the function outputs true, otherwise false.
    	public static bool LinePlaneIntersection(out Vector3 intersection, Vector3 linePoint, Vector3 lineVec, Vector3 planeNormal, Vector3 planePoint){
     
    		float length;
    		float dotNumerator;
    		float dotDenominator;
    		Vector3 vector;
    		intersection = Vector3.zero;
     
    		//calculate the distance between the linePoint and the line-plane intersection point
    		dotNumerator = Vector3.Dot((planePoint - linePoint), planeNormal);
    		dotDenominator = Vector3.Dot(lineVec, planeNormal);
     
    		//line and plane are not parallel
    		if(dotDenominator != 0.0f){
    			length =  dotNumerator / dotDenominator;
     
    			//create a vector from the linePoint to the intersection point
    			vector = SetVectorLength(lineVec, length);
     
    			//get the coordinates of the line-plane intersection point
    			intersection = linePoint + vector;	
     
    			return true;	
    		}
     
    		//output not valid
    		else{
    			return false;
    		}
    	}
     
    	//Calculate the intersection point of two lines. Returns true if lines intersect, otherwise false.
    	//Note that in 3d, two lines do not intersect most of the time. So if the two lines are not in the 
    	//same plane, use ClosestPointsOnTwoLines() instead.
    	public static bool LineLineIntersection(out Vector3 intersection, Vector3 line1Start, Vector3 line1End, Vector3 line2Start, Vector3 line2End){
     
    		intersection = Vector3.zero;
     
    		Vector3 lineVec3 = line2Start - line1Start;
    		Vector3 crossVec1and2 = Vector3.Cross(line1End, line2End);
    		Vector3 crossVec3and2 = Vector3.Cross(lineVec3, line2End);
     
    		float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);
     
    		//Lines are not coplanar. Take into account rounding errors.
    		if((planarFactor >= 0.00001f) || (planarFactor <= -0.00001f)){
    			return false;
    		}
     
    		//Note: sqrMagnitude does x*x+y*y+z*z on the input vector.
    		float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;
     
    		if((s >= 0.0f) && (s <= 1.0f)){
     
    			intersection = line1Start + (line1End * s);
    			return true;
    		}
     
    		else{
    			return false;       
    		}
    	}

        public static bool LineLineIntersection(out Vector2 intersection, Vector2 line1Start, Vector2 line1End, Vector2 line2Start, Vector2 line2End){
            
            intersection = Vector2.zero;
            
            float s1_x, s1_y, s2_x, s2_y;
            s1_x = line1End.x - line1Start.x;     s1_y = line1End.y - line1Start.y;
            s2_x = line2End.x - line2Start.x;     s2_y = line2End.y - line2Start.y;
            
            float s, t;
            s = (-s1_y * (line1Start.x - line2Start.x) + s1_x * (line1Start.y - line2Start.y)) / (-s2_x * s1_y + s1_x * s2_y);
            t = ( s2_x * (line1Start.y - line2Start.y) - s2_y * (line1Start.x - line2Start.x)) / (-s2_x * s1_y + s1_x * s2_y);
            
            intersection.x = line1Start.x + (t * s1_x);
            intersection.y = line1Start.y + (t * s1_y);
            
            if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
            {
                return true;
            }
            
            return false; // No collision
        }

        public static float ClosestDistanceToLine(Vector2 lineStart, Vector2 lineEnd, Vector2 point)
        {
            Vector2 point2 = new Vector2(lineEnd.x - lineStart.x, lineEnd.y - lineStart.y);
            float dot = point2.x * point2.x + point2.y * point2.y;
            float magnitude = ((point.x - lineStart.x) * point2.x + (point.y - lineStart.y) * point2.y);
            if(dot != 0f)
                magnitude = magnitude / dot;
            
            if (magnitude > 1)
                magnitude = 1;
            else if (magnitude < 0)
                magnitude = 0;
            
            float x = lineStart.x + magnitude * point2.x;
            float y = lineStart.y + magnitude * point2.y;
            
            float dx = x - point.x;
            float dy = y - point.y;

            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        public static float ClosestDistanceToPolygon(Vector2[] points, Vector2 point)
        {
            int pointsLength = points.Length;
            if (pointsLength <= 1)
            {
                return 0f;
            } else if (pointsLength == 2)
            {
                return ClosestDistanceToLine(points [0], points [1], point);
            } 

            float minDistance = float.MaxValue, currentDistance;
            Vector2 lastPoint = points[0];
            for(int i = 1; i < pointsLength; i++)
            {
                currentDistance = ClosestDistanceToLine(lastPoint, points[i], point);
                if(currentDistance < minDistance)
                    minDistance = currentDistance;
                lastPoint = points[i];
            }

            return minDistance;
        }
        public static float ClosestPointToPolygon(Vector2[] points, Vector2 point, out Vector2 pointOnLine)
        {
            float pointIndex;
            return ClosestPointToPolygon(points, point, out pointOnLine, out pointIndex);
        }

        public static float ClosestPointToPolygon(Vector2[] points, Vector2 point, out Vector2 pointOnLine, out float pointIndex)
        {
            float closestDistance = 0f;
            pointOnLine = Vector2.zero;
            pointIndex = 0f;

            if (points == null)
            {
                return 0f;
            }

            int pointsLength = points.Length;
            if (pointsLength == 0)
            {
                return 0f;
            }
            else if (pointsLength == 1)
            {
                pointOnLine = points[0];
                return 0f;
            } else if (pointsLength == 2)
            {
                closestDistance = ClosestPointToLine(points [0], points [1], point, out pointOnLine);
                pointIndex = Vector2.Distance(points [0], pointOnLine) / Vector2.Distance(points [0], points [1]);
                return closestDistance;
            }
            
            float minDistance = float.MaxValue, currentDistance;
            Vector2 lastPoint = points[0];
            Vector2 tempPointOnLine;
            for(int i = 1; i < pointsLength; i++)
            {
                currentDistance = ClosestPointToLine(lastPoint, points[i], point, out tempPointOnLine);
                if(currentDistance < minDistance)
                {
                    pointOnLine = tempPointOnLine;
                    minDistance = currentDistance;
                    pointIndex = (i - 1) + Vector2.Distance(lastPoint, pointOnLine) / Vector2.Distance(lastPoint, points[i]);
                }
                lastPoint = points[i];
            }
            
            return minDistance;
        }

        public static float ClosestPointToLine(Vector2 lineStart, Vector2 lineEnd, Vector2 point, out Vector2 pointOnLine)
        {
            Vector2 point2 = new Vector2(lineEnd.x - lineStart.x, lineEnd.y - lineStart.y);
            float dot = point2.x * point2.x + point2.y * point2.y;
            float magnitude = ((point.x - lineStart.x) * point2.x + (point.y - lineStart.y) * point2.y);
            if(dot != 0f)
                magnitude = magnitude / dot;
            
            if (magnitude > 1)
                magnitude = 1;
            else if (magnitude < 0)
                magnitude = 0;
            
            pointOnLine.x = lineStart.x + magnitude * point2.x;
            pointOnLine.y = lineStart.y + magnitude * point2.y;
            
            float dx = pointOnLine.x - point.x;
            float dy = pointOnLine.y - point.y;
            
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        public static float ClosestPointToLine(Vector3 lineStart, Vector3 lineEnd, Vector3 point, out Vector3 pointOnLine)
        {
            Vector3 point2 = new Vector3(lineEnd.x - lineStart.x, lineEnd.y - lineStart.y, lineEnd.z - lineStart.z);
            float dot = point2.x * point2.x + point2.y * point2.y + point2.z * point2.z;
            float magnitude = ((point.x - lineStart.x) * point2.x + (point.y - lineStart.y) * point2.y+ (point.z - lineStart.z) * point2.z);
            if(dot != 0f)
                magnitude = magnitude / dot;
            
            if (magnitude > 1)
                magnitude = 1;
            else if (magnitude < 0)
                magnitude = 0;
            
            pointOnLine.x = lineStart.x + magnitude * point2.x;
            pointOnLine.y = lineStart.y + magnitude * point2.y;
            pointOnLine.z = lineStart.z + magnitude * point2.z;

            float dx = pointOnLine.x - point.x;
            float dy = pointOnLine.y - point.y;
            float dz = pointOnLine.z - point.z;
            
            return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /*
        public static float ClosestPointOnCubicCurve(Vector3 start, Vector3 startHandle, Vector3 endHandle, Vector3 end, Vector3 a_CheckPoint)
        {
            Vector3[] points = new Vector3{start, startHandle, endHandle, end};
            points = SubdivideCubicCurve(points);
            points = SubdivideCubicCurve(points);
            points = SubdivideCubicCurve(points);
        }
        */

        public static Vector3 DeCasteljau (Vector3 start, Vector3 startHandle, Vector3 endHandle, Vector3 end, float progress)
        {
            Vector3 a = start + progress * (startHandle - start);
            Vector3 b = startHandle + progress * (endHandle - startHandle);
            Vector3 c = endHandle + progress * (end - endHandle);
            Vector3 x = a + progress * (b - a);
            Vector3 y = b + progress * (c - b);
            return x + progress * (y - x);
        }

        /*
        public static float ClosestPointOnCubicCurve(Vector3 a_StartPoint, Vector3 a_EndPoint, Vector3 a_CheckPoint)
        {
            Vector3 m_TempVector1 = a_CheckPoint - a_StartPoint;
            Vector3 m_TempVector2 = (a_EndPoint - a_StartPoint).normalized;
            
            float m_Distance = (a_StartPoint - a_EndPoint).magnitude;
            float m_DotProduct = Vector3.Dot(m_TempVector2, m_TempVector1); 
            
            if (m_DotProduct <= 0.0f)
            {
                ClosestPointInfo m_ClosestPoint;
                m_ClosestPoint.m_Position   = a_StartPoint;
                m_ClosestPoint.m_ReturnType = ReturnType.StartPoint;
                
                return m_ClosestPoint;
            }
            
            if (m_DotProduct >= m_Distance)
            {
                ClosestPointInfo m_ClosestPoint;
                m_ClosestPoint.m_Position   = a_EndPoint;
                m_ClosestPoint.m_ReturnType = ReturnType.EndPoint;
                
                return m_ClosestPoint;
            }
            
            Vector3 m_TempVector3   = m_TempVector2 * m_DotProduct;
            
            ClosestPointInfo m_ClosestPoint1;
            m_ClosestPoint1.m_Position   = a_StartPoint + m_TempVector3;
            m_ClosestPoint1.m_ReturnType = ReturnType.MiddlePoint;
            
            return m_ClosestPoint1;
        }
        */

    	//Two non-parallel lines which may or may not touch each other have a point on each line which are closest
    	//to each other. This function finds those two points. If the lines are not parallel, the function 
    	//outputs true, otherwise false.
    	public static bool ClosestPointsOnTwoLines(out Vector3 closestPointLine1, out Vector3 closestPointLine2, Vector3 line1Start, Vector3 line1End, Vector3 line2Start, Vector3 line2End){
     
    		closestPointLine1 = Vector3.zero;
    		closestPointLine2 = Vector3.zero;
     
    		float a = Vector3.Dot(line1End, line1End);
    		float b = Vector3.Dot(line1End, line2End);
    		float e = Vector3.Dot(line2End, line2End);
     
    		float d = a*e - b*b;
     
    		//lines are not parallel
    		if(d != 0.0f){
     
    			Vector3 r = line1Start - line2Start;
    			float c = Vector3.Dot(line1End, r);
    			float f = Vector3.Dot(line2End, r);
     
    			float s = (b*f - c*e) / d;
    			float t = (a*f - c*b) / d;
     
    			closestPointLine1 = line1Start + line1End * s;
    			closestPointLine2 = line2Start + line2End * t;
     
    			return true;
    		}
     
    		else{
    			return false;
    		}
    	}	
     
    	//This function returns a point which is a projection from a point to a line.
    	//The line is regarded infinite. If the line is finite, use ProjectPointOnLineSegment() instead.
    	public static Vector3 ProjectPointOnLine(Vector3 linePoint, Vector3 lineVec, Vector3 point){		
     
    		//get vector from point on line to point in space
    		Vector3 linePointToPoint = point - linePoint;
     
    		float t = Vector3.Dot(linePointToPoint, lineVec);
     
    		return linePoint + lineVec * t;
    	}
     
    	//This function returns a point which is a projection from a point to a line segment.
    	//If the projected point lies outside of the line segment, the projected point will 
    	//be clamped to the appropriate line edge.
    	//If the line is infinite instead of a segment, use ProjectPointOnLine() instead.
    	public static Vector3 ProjectPointOnLineSegment(Vector3 line1Start, Vector3 line2Start, Vector3 point){
     
    		Vector3 vector = line2Start - line1Start;
     
    		Vector3 projectedPoint = ProjectPointOnLine(line1Start, vector.normalized, point);
     
    		int side = PointOnWhichSideOfLineSegment(line1Start, line2Start, projectedPoint);
     
    		//The projected point is on the line segment
    		if(side == 0){
     
    			return projectedPoint;
    		}
     
    		if(side == 1){
     
    			return line1Start;
    		}
     
    		if(side == 2){
     
    			return line2Start;
    		}
     
    		//output is invalid
    		return Vector3.zero;
    	}	
     
    	//This function returns a point which is a projection from a point to a plane.
    	public static Vector3 ProjectPointOnPlane(Vector3 planeNormal, Vector3 planePoint, Vector3 point){
     
    		float distance;
    		Vector3 translationVector;
     
    		//First calculate the distance from the point to the plane:
    		distance = SignedDistancePlanePoint(planeNormal, planePoint, point);
     
    		//Reverse the sign of the distance
    		distance *= -1;
     
    		//Get a translation vector
    		translationVector = SetVectorLength(planeNormal, distance);
     
    		//Translate the point to form a projection
    		return point + translationVector;
    	}	
     
    	//Projects a vector onto a plane. The output is not normalized.
    	public static Vector3 ProjectVectorOnPlane(Vector3 planeNormal, Vector3 vector){
     
    		return vector - (Vector3.Dot(vector, planeNormal) * planeNormal);
    	}
     
    	//Get the shortest distance between a point and a plane. The output is signed so it holds information
    	//as to which side of the plane normal the point is.
    	public static float SignedDistancePlanePoint(Vector3 planeNormal, Vector3 planePoint, Vector3 point){
     
    		return Vector3.Dot(planeNormal, (point - planePoint));
    	}	
     
    	//This function calculates a signed (+ or - sign instead of being ambiguous) dot product. It is basically used
    	//to figure out whether a vector is positioned to the left or right of another vector. The way this is done is
    	//by calculating a vector perpendicular to one of the vectors and using that as a reference. This is because
    	//the result of a dot product only has signed information when an angle is transitioning between more or less
    	//then 90 degrees.
    	public static float SignedDotProduct(Vector3 vectorA, Vector3 vectorB, Vector3 normal){
     
    		Vector3 perpVector;
    		float dot;
     
    		//Use the geometry object normal and one of the input vectors to calculate the perpendicular vector
    		perpVector = Vector3.Cross(normal, vectorA);
     
    		//Now calculate the dot product between the perpendicular vector (perpVector) and the other input vector
    		dot = Vector3.Dot(perpVector, vectorB);
     
    		return dot;
    	}
     
    	public static float SignedVectorAngle(Vector3 referenceVector, Vector3 otherVector, Vector3 normal)
    	{
    		Vector3 perpVector;
    		float angle;
     
    		//Use the geometry object normal and one of the input vectors to calculate the perpendicular vector
    		perpVector = Vector3.Cross(normal, referenceVector);
     
    		//Now calculate the dot product between the perpendicular vector (perpVector) and the other input vector
    		angle = Vector3.Angle(referenceVector, otherVector);
    		angle *= Mathf.Sign(Vector3.Dot(perpVector, otherVector));
     
    		return angle;
    	}
     
    	//Calculate the angle between a vector and a plane. The plane is made by a normal vector.
    	//Output is in radians.
    	public static float AngleVectorPlane(Vector3 vector, Vector3 normal){
     
    		float dot;
    		float angle;
     
    		//calculate the the dot product between the two input vectors. This gives the cosine between the two vectors
    		dot = Vector3.Dot(vector, normal);
     
    		//this is in radians
    		angle = (float)Math.Acos(dot);
     
    		return 1.570796326794897f - angle; //90 degrees - angle
    	}
     
    	//Calculate the dot product as an angle
    	public static float DotProductAngle(Vector3 vec1, Vector3 vec2){
     
    		double dot;
    		double angle;
     
    		//get the dot product
    		dot = Vector3.Dot(vec1, vec2);
     
    		//Clamp to prevent NaN error. Shouldn't need this in the first place, but there could be a rounding error issue.
    		if(dot < -1.0f){
    			dot = -1.0f;
    		}							
    		if(dot > 1.0f){
    			dot =1.0f;
    		}
     
    		//Calculate the angle. The output is in radians
    		//This step can be skipped for optimization...
    		angle = Math.Acos(dot);
     
    		return (float)angle;
    	}
     
    	//Convert a plane defined by 3 points to a plane defined by a vector and a point. 
    	//The plane point is the middle of the triangle defined by the 3 points.
    	public static void PlaneFrom3Points(out Vector3 planeNormal, out Vector3 planePoint, Vector3 pointA, Vector3 pointB, Vector3 pointC){
     
    		planeNormal = Vector3.zero;
    		planePoint = Vector3.zero;
     
    		//Make two vectors from the 3 input points, originating from point A
    		Vector3 AB = pointB - pointA;
    		Vector3 AC = pointC - pointA;
     
    		//Calculate the normal
    		planeNormal = Vector3.Normalize(Vector3.Cross(AB, AC));
     
    		//Get the points in the middle AB and AC
    		Vector3 middleAB = pointA + (AB / 2.0f);
    		Vector3 middleAC = pointA + (AC / 2.0f);
     
    		//Get vectors from the middle of AB and AC to the point which is not on that line.
    		Vector3 middleABtoC = pointC - middleAB;
    		Vector3 middleACtoB = pointB - middleAC;
     
    		//Calculate the intersection between the two lines. This will be the center 
    		//of the triangle defined by the 3 points.
    		//We could use LineLineIntersection instead of ClosestPointsOnTwoLines but due to rounding errors 
    		//this sometimes doesn't work.
    		Vector3 temp;
    		ClosestPointsOnTwoLines(out planePoint, out temp, middleAB, middleABtoC, middleAC, middleACtoB);
    	}
     
    	//Returns the forward vector of a quaternion
    	public static Vector3 GetForwardVector(Quaternion q){
     
    		return q * Vector3.forward;
    	}
     
    	//Returns the up vector of a quaternion
    	public static Vector3 GetUpVector(Quaternion q){
     
    		return q * Vector3.up;
    	}
     
    	//Returns the right vector of a quaternion
    	public static Vector3 GetRightVector(Quaternion q){
     
    		return q * Vector3.right;
    	}
     
    	//Gets a quaternion from a matrix
    	public static Quaternion QuaternionFromMatrix(Matrix4x4 m){ 
     
    		return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1)); 
    	}
     
    	//Gets a position from a matrix
    	public static Vector3 PositionFromMatrix(Matrix4x4 m){
     
    		Vector4 vector4Position = m.GetColumn(3);
    		return new Vector3(vector4Position.x, vector4Position.y, vector4Position.z);
    	}
     
    	//This is an alternative for Quaternion.LookRotation. Instead of aligning the forward and up vector of the game 
    	//object with the input vectors, a custom direction can be used instead of the fixed forward and up vectors.
    	//alignWithVector and alignWithNormal are in world space.
    	//customForward and customUp are in object space.
    	//Usage: use alignWithVector and alignWithNormal as if you are using the default LookRotation function.
    	//Set customForward and customUp to the vectors you wish to use instead of the default forward and up vectors.
    	public static void LookRotationExtended(ref GameObject gameObjectInOut, Vector3 alignWithVector, Vector3 alignWithNormal, Vector3 customForward, Vector3 customUp){
     
    		//Set the rotation of the destination
    		Quaternion rotationA = Quaternion.LookRotation(alignWithVector, alignWithNormal);		
     
    		//Set the rotation of the custom normal and up vectors. 
    		//When using the default LookRotation function, this would be hard coded to the forward and up vector.
    		Quaternion rotationB = Quaternion.LookRotation(customForward, customUp);
     
    		//Calculate the rotation
    		gameObjectInOut.transform.rotation =  rotationA * Quaternion.Inverse(rotationB);
    	}
     
    	//With this function you can align a triangle of an object with any transform.
    	//Usage: gameObjectInOut is the game object you want to transform.
    	//alignWithVector, alignWithNormal, and alignWithPosition is the transform with which the triangle of the object should be aligned with.
    	//triangleForward, triangleNormal, and trianglePosition is the transform of the triangle from the object.
    	//alignWithVector, alignWithNormal, and alignWithPosition are in world space.
    	//triangleForward, triangleNormal, and trianglePosition are in object space.
    	//trianglePosition is the mesh position of the triangle. The effect of the scale of the object is handled automatically.
    	//trianglePosition can be set at any position, it does not have to be at a vertex or in the middle of the triangle.
    	public static void PreciseAlign(ref GameObject gameObjectInOut, Vector3 alignWithVector, Vector3 alignWithNormal, Vector3 alignWithPosition, Vector3 triangleForward, Vector3 triangleNormal, Vector3 trianglePosition){
     
    		//Set the rotation.
    		LookRotationExtended(ref gameObjectInOut, alignWithVector, alignWithNormal, triangleForward, triangleNormal);
     
    		//Get the world space position of trianglePosition
    		Vector3 trianglePositionWorld = gameObjectInOut.transform.TransformPoint(trianglePosition);
     
    		//Get a vector from trianglePosition to alignWithPosition
    		Vector3 translateVector = alignWithPosition - trianglePositionWorld;
     
    		//Now transform the object so the triangle lines up correctly.
    		gameObjectInOut.transform.Translate(translateVector, Space.World);
    	}
     
     
    	//Convert a position, direction, and normal vector to a transform
    	void VectorsToTransform(ref GameObject gameObjectInOut, Vector3 positionVector, Vector3 directionVector, Vector3 normalVector){
     
    		gameObjectInOut.transform.position = positionVector;			
    		gameObjectInOut.transform.rotation = Quaternion.LookRotation(directionVector, normalVector);
    	}
     
    	//This function finds out on which side of a line segment the point is located.
    	//The point is assumed to be on a line created by line1Start and line2Start. If the point is not on
    	//the line segment, project it on the line using ProjectPointOnLine() first.
    	//Returns 0 if point is on the line segment.
    	//Returns 1 if point is outside of the line segment and located on the side of line1Start.
    	//Returns 2 if point is outside of the line segment and located on the side of line2Start.
    	public static int PointOnWhichSideOfLineSegment(Vector3 line1Start, Vector3 line2Start, Vector3 point){
     
    		Vector3 lineVec = line2Start - line1Start;
    		Vector3 pointVec = point - line1Start;
     
    		float dot = Vector3.Dot(pointVec, lineVec);
     
    		//point is on side of line2Start, compared to line1Start
    		if(dot > 0){
     
    			//point is on the line segment
    			if(pointVec.magnitude <= lineVec.magnitude){
     
    				return 0;
    			}
     
    			//point is not on the line segment and it is on the side of line2Start
    			else{
     
    				return 2;
    			}
    		}
     
    		//Point is not on side of line2Start, compared to line1Start.
    		//Point is not on the line segment and it is on the side of line1Start.
    		else{
     
    			return 1;
    		}
    	}
     
     
    	//Returns the pixel distance from the mouse pointer to a line.
    	//Alternative for HandleUtility.DistanceToLine(). Works both in Editor mode and Play mode.
    	//Do not call this function from OnGUI() as the mouse position will be wrong.
    	public static float MouseDistanceToLine(Vector3 line1Start, Vector3 line2Start){
     
    		Camera currentCamera;
    		Vector3 mousePosition;
     
    #if UNITY_EDITOR
    		if(Camera.current != null){
     
    			currentCamera = Camera.current;
    		}
     
    		else{
     
    			currentCamera = Camera.main;
    		}
     
    		//convert format because y is flipped
    		mousePosition = new Vector3(Event.current.mousePosition.x, currentCamera.pixelHeight - Event.current.mousePosition.y, 0f);
     
    #else
    		currentCamera = Camera.main;
    		mousePosition = Input.mousePosition;
    #endif
     
    		Vector3 screenPos1 = currentCamera.WorldToScreenPoint(line1Start);
    		Vector3 screenPos2 = currentCamera.WorldToScreenPoint(line2Start);
    		Vector3 projectedPoint = ProjectPointOnLineSegment(screenPos1, screenPos2, mousePosition);
     
    		//set z to zero
    		projectedPoint = new Vector3(projectedPoint.x, projectedPoint.y, 0f);
     
    		Vector3 vector = projectedPoint - mousePosition;
    		return vector.magnitude;
    	}
     
     
    	//Returns the pixel distance from the mouse pointer to a camera facing circle.
    	//Alternative for HandleUtility.DistanceToCircle(). Works both in Editor mode and Play mode.
    	//Do not call this function from OnGUI() as the mouse position will be wrong.
    	//If you want the distance to a point instead of a circle, set the radius to 0.
    	public static float MouseDistanceToCircle(Vector3 point, float radius){
     
    		Camera currentCamera;
    		Vector3 mousePosition;
     
    #if UNITY_EDITOR
    		if(Camera.current != null){
     
    			currentCamera = Camera.current;
    		}
     
    		else{
     
    			currentCamera = Camera.main;
    		}
     
    		//convert format because y is flipped
    		mousePosition = new Vector3(Event.current.mousePosition.x, currentCamera.pixelHeight - Event.current.mousePosition.y, 0f);
    #else
    		currentCamera = Camera.main;
    		mousePosition = Input.mousePosition;
    #endif
     
    		Vector3 screenPos = currentCamera.WorldToScreenPoint(point);
     
    		//set z to zero
    		screenPos = new Vector3(screenPos.x, screenPos.y, 0f);
     
    		Vector3 vector = screenPos - mousePosition;
    		float fullDistance = vector.magnitude;
    		float circleDistance = fullDistance - radius;
     
    		return circleDistance;
    	}
     
    	//Returns true if a line segment (made up of line1Start and line2Start) is fully or partially in a rectangle
    	//made up of RectA to RectD. The line segment is assumed to be on the same plane as the rectangle. If the line is 
    	//not on the plane, use ProjectPointOnPlane() on line1Start and line2Start first.
    	public static bool IsLineInRectangle(Vector3 line1Start, Vector3 line2Start, Vector3 rectA, Vector3 rectB, Vector3 rectC, Vector3 rectD){
     
    		bool pointAInside = false;
    		bool pointBInside = false;
     
    		pointAInside = IsPointInRectangle(line1Start, rectA, rectC, rectB, rectD);
     
    		if(!pointAInside){
     
    			pointBInside = IsPointInRectangle(line2Start, rectA, rectC, rectB, rectD);
    		}
     
    		//none of the points are inside, so check if a line is crossing
    		if(!pointAInside && !pointBInside){
     
    			bool lineACrossing = AreLineSegmentsCrossing(line1Start, line2Start, rectA, rectB);
    			bool lineBCrossing = AreLineSegmentsCrossing(line1Start, line2Start, rectB, rectC);
    			bool lineCCrossing = AreLineSegmentsCrossing(line1Start, line2Start, rectC, rectD);
    			bool lineDCrossing = AreLineSegmentsCrossing(line1Start, line2Start, rectD, rectA);
     
    			if(lineACrossing || lineBCrossing || lineCCrossing || lineDCrossing){
     
    				return true;
    			}
     
    			else{
     
    				return false;
    			}
    		}
     
    		else{
     
    			return true;
    		}
    	}
     
    	//Returns true if "point" is in a rectangle mad up of RectA to RectD. The line point is assumed to be on the same 
    	//plane as the rectangle. If the point is not on the plane, use ProjectPointOnPlane() first.
    	public static bool IsPointInRectangle(Vector3 point, Vector3 rectA, Vector3 rectC, Vector3 rectB, Vector3 rectD){
     
    		Vector3 vector;
    		Vector3 linePoint;
     
    		//get the center of the rectangle
    		vector = rectC - rectA;
    		float size = -(vector.magnitude / 2f);
    		vector = AddVectorLength(vector, size);
    		Vector3 middle = rectA + vector;
     
    		Vector3 xVector = rectB - rectA;
    		float width = xVector.magnitude / 2f;
     
    		Vector3 yVector = rectD - rectA;
    		float height = yVector.magnitude / 2f;
     
    		linePoint = ProjectPointOnLine(middle, xVector.normalized, point);
    		vector = linePoint - point;
    		float yDistance = vector.magnitude;
     
    		linePoint = ProjectPointOnLine(middle, yVector.normalized, point);
    		vector = linePoint - point;
    		float xDistance = vector.magnitude;
     
    		if((xDistance <= width) && (yDistance <= height)){
     
    			return true;
    		}
     
    		else{
     
    			return false;
    		}
    	}
     
    	//Returns true if line segment made up of pointA1 and pointA2 is crossing line segment made up of
    	//pointB1 and pointB2. The two lines are assumed to be in the same plane.
    	public static bool AreLineSegmentsCrossing (Vector3 pointA1, Vector3 pointA2, Vector3 pointB1, Vector3 pointB2)
    	{
     
    		Vector3 closestPointA;
    		Vector3 closestPointB;
    		int sideA;
    		int sideB;
     
    		Vector3 lineVecA = pointA2 - pointA1;
    		Vector3 lineVecB = pointB2 - pointB1;
     
    		bool valid = ClosestPointsOnTwoLines (out closestPointA, out closestPointB, pointA1, lineVecA.normalized, pointB1, lineVecB.normalized); 
     
    		//lines are not parallel
    		if (valid) {
     
    			sideA = PointOnWhichSideOfLineSegment (pointA1, pointA2, closestPointA);
    			sideB = PointOnWhichSideOfLineSegment (pointB1, pointB2, closestPointB);
     
    			if ((sideA == 0) && (sideB == 0)) {
     
    				return true;
    			} else {
     
    				return false;
    			}
    		} else {
     
    			return false;
    		}
    	}
    	
    	public static Bounds GetWorldBounds (Transform transform, Bounds bounds)
    	{
    		Bounds output = new Bounds (transform.TransformPoint (bounds.center), Vector3.zero);
    		Vector3 point0 = new Vector3 (bounds.size.x, bounds.size.y, bounds.size.z) * 0.5f;
    		Vector3 point1 = new Vector3 (-bounds.size.x, bounds.size.y, bounds.size.z) * 0.5f;
    		Vector3 point2 = new Vector3 (bounds.size.x, -bounds.size.y, bounds.size.z) * 0.5f;
    		Vector3 point3 = new Vector3 (-bounds.size.x, -bounds.size.y, bounds.size.z) * 0.5f;
    		Vector3 point4 = new Vector3 (bounds.size.x, bounds.size.y, -bounds.size.z) * 0.5f;
    		Vector3 point5 = new Vector3 (-bounds.size.x, bounds.size.y, -bounds.size.z) * 0.5f;
    		Vector3 point6 = new Vector3 (bounds.size.x, -bounds.size.y, -bounds.size.z) * 0.5f;
    		Vector3 point7 = new Vector3 (-bounds.size.x, -bounds.size.y, -bounds.size.z) * 0.5f;

    		output.Encapsulate (transform.TransformPoint (bounds.center + point0));
    		output.Encapsulate (transform.TransformPoint (bounds.center + point1));
    		output.Encapsulate (transform.TransformPoint (bounds.center + point2));
    		output.Encapsulate (transform.TransformPoint (bounds.center + point3));
    		output.Encapsulate (transform.TransformPoint (bounds.center + point4));
    		output.Encapsulate (transform.TransformPoint (bounds.center + point5));
    		output.Encapsulate (transform.TransformPoint (bounds.center + point6));
    		output.Encapsulate (transform.TransformPoint (bounds.center + point7));
    		return output;
    	}

        public static bool IsPolygonsIntersecting(Vector2[] a, Vector2[] b)
        {
            foreach (Vector2[] polygon in new[] { a, b })
            {
                for (int i1 = 0; i1 < polygon.Length; i1++)
                {
                    int i2 = (i1 + 1) % polygon.Length;
                    Vector2 p1 = polygon[i1];
                    Vector2 p2 = polygon[i2];

                    Vector2 normal = new Vector2(p2.y - p1.y, p1.x - p2.x);
                    
                    float minA = float.MaxValue, maxA = float.MinValue;
                    foreach (Vector2 p in a)
                    {
                        float projected = normal.x * p.x + normal.y * p.y;
                        if (projected < minA)
                            minA = projected;
                        if (projected > maxA)
                            maxA = projected;
                    }
                    
                    float minB = float.MaxValue, maxB = float.MinValue;
                    foreach (Vector2 p in b)
                    {
                        float projected = normal.x * p.x + normal.y * p.y;
                        if (projected < minB)
                            minB = projected;
                        if (projected > maxB)
                            maxB = projected;
                    }
                    
                    if (maxA < minB || maxB < minA)
                        return false;
                }
            }
            return true;
        }

        public static bool PolygonContainsPoint (Vector2[] polyPoints, Vector2 point) { 
            int polyPointsLength = polyPoints.Length;
            int j = polyPointsLength-1; 
            bool inside = false;
            for (int i = 0; i < polyPointsLength; j = i++) { 
                if ( ((polyPoints[i].y <= point.y && point.y < polyPoints[j].y) || (polyPoints[j].y <= point.y && point.y < polyPoints[i].y)) && 
                    (point.x < (polyPoints[j].x - polyPoints[i].x) * (point.y - polyPoints[i].y) / (polyPoints[j].y - polyPoints[i].y) + polyPoints[i].x)) 
                    inside = !inside; 
            } 
            return inside; 
        }

        public static bool PolygonContainsPoint (List<Vector2> polyPoints, Vector2 point) { 
            int polyPointsCount = polyPoints.Count;
            int j = polyPointsCount-1; 
            bool inside = false;
            for (int i = 0; i < polyPointsCount; j = i++)
            { 
                if (((polyPoints [i].y <= point.y && point.y < polyPoints [j].y) || (polyPoints [j].y <= point.y && point.y < polyPoints [i].y)) && 
                    (point.x < (polyPoints [j].x - polyPoints [i].x) * (point.y - polyPoints [i].y) / (polyPoints [j].y - polyPoints [i].y) + polyPoints [i].x)) 
                    inside = !inside; 
            } 
            return inside; 
        }
    }
}