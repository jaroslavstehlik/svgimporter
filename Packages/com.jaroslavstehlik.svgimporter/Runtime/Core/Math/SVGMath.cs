

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

    	//increase or decrease the length of vector by size
     
    	//create a vector of direction "vector" with length "size"
     
     
    	//caclulate the rotational difference from A to B
     
    	//Find the line of intersection between two planes.	The planes are defined by a normal and a point on that plane.
    	//The outputs are a point on the line and a vector which indicates it's direction. If the planes are not parallel, 
    	//the function outputs true, otherwise false.
     
    	//Get the intersection between a line and a plane. 
    	//If the line and plane are not parallel, the function outputs true, otherwise false.
     
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



    	//Two non-parallel lines which may or may not touch each other have a point on each line which are closest
    	//to each other. This function finds those two points. If the lines are not parallel, the function 
    	//outputs true, otherwise false.
     
    	//This function returns a point which is a projection from a point to a line.
    	//The line is regarded infinite. If the line is finite, use ProjectPointOnLineSegment() instead.
     
    	//This function returns a point which is a projection from a point to a line segment.
    	//If the projected point lies outside of the line segment, the projected point will 
    	//be clamped to the appropriate line edge.
    	//If the line is infinite instead of a segment, use ProjectPointOnLine() instead.
     
    	//This function returns a point which is a projection from a point to a plane.
     
    	//Projects a vector onto a plane. The output is not normalized.
     
    	//Get the shortest distance between a point and a plane. The output is signed so it holds information
    	//as to which side of the plane normal the point is.
     
    	//This function calculates a signed (+ or - sign instead of being ambiguous) dot product. It is basically used
    	//to figure out whether a vector is positioned to the left or right of another vector. The way this is done is
    	//by calculating a vector perpendicular to one of the vectors and using that as a reference. This is because
    	//the result of a dot product only has signed information when an angle is transitioning between more or less
    	//then 90 degrees.
     
    	//Calculate the angle between a vector and a plane. The plane is made by a normal vector.
    	//Output is in radians.
     
    	//Calculate the dot product as an angle
     
    	//Convert a plane defined by 3 points to a plane defined by a vector and a point. 
    	//The plane point is the middle of the triangle defined by the 3 points.
     
    	//Returns the forward vector of a quaternion
     
    	//Returns the up vector of a quaternion
     
    	//Returns the right vector of a quaternion
     
    	//Gets a quaternion from a matrix
     
    	//Gets a position from a matrix
     
    	//This is an alternative for Quaternion.LookRotation. Instead of aligning the forward and up vector of the game 
    	//object with the input vectors, a custom direction can be used instead of the fixed forward and up vectors.
    	//alignWithVector and alignWithNormal are in world space.
    	//customForward and customUp are in object space.
    	//Usage: use alignWithVector and alignWithNormal as if you are using the default LookRotation function.
    	//Set customForward and customUp to the vectors you wish to use instead of the default forward and up vectors.
     
    	//With this function you can align a triangle of an object with any transform.
    	//Usage: gameObjectInOut is the game object you want to transform.
    	//alignWithVector, alignWithNormal, and alignWithPosition is the transform with which the triangle of the object should be aligned with.
    	//triangleForward, triangleNormal, and trianglePosition is the transform of the triangle from the object.
    	//alignWithVector, alignWithNormal, and alignWithPosition are in world space.
    	//triangleForward, triangleNormal, and trianglePosition are in object space.
    	//trianglePosition is the mesh position of the triangle. The effect of the scale of the object is handled automatically.
    	//trianglePosition can be set at any position, it does not have to be at a vertex or in the middle of the triangle.
     
     
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
     
     
    	//Returns the pixel distance from the mouse pointer to a line.
    	//Alternative for HandleUtility.DistanceToLine(). Works both in Editor mode and Play mode.
    	//Do not call this function from OnGUI() as the mouse position will be wrong.
     
     
    	//Returns the pixel distance from the mouse pointer to a camera facing circle.
    	//Alternative for HandleUtility.DistanceToCircle(). Works both in Editor mode and Play mode.
    	//Do not call this function from OnGUI() as the mouse position will be wrong.
    	//If you want the distance to a point instead of a circle, set the radius to 0.
     
    	//Returns true if a line segment (made up of line1Start and line2Start) is fully or partially in a rectangle
    	//made up of RectA to RectD. The line segment is assumed to be on the same plane as the rectangle. If the line is 
    	//not on the plane, use ProjectPointOnPlane() on line1Start and line2Start first.
     
    	//Returns true if "point" is in a rectangle mad up of RectA to RectD. The line point is assumed to be on the same 
    	//plane as the rectangle. If the point is not on the plane, use ProjectPointOnPlane() first.
     
    	//Returns true if line segment made up of pointA1 and pointA2 is crossing line segment made up of
    	//pointB1 and pointB2. The two lines are assumed to be in the same plane.
    }
}