using UnityEngine;
using System.Collections;

public class Controller2D : RaycastController
{
    float maxClimbAngle = 80;
    float maxDescendAngle = 75;

    public CollisionInfo collisions;

    public override void Start() {
        base.Start();
            }

    public void Move(Vector3 velocity)
    {
        UpdateRaycastOrigins();
        collisions.Reset();
        collisions.velocityOld = velocity;

        //handle collisions
        if(velocity.y < 0)
        {
            DescendSlope(ref velocity);
        }
        if(velocity.x != 0)
        {
            HorizontalCollisions(ref velocity);
        }
        if(velocity.y != 0)
        {
            VerticalCollisions(ref velocity);
        }
        //move
        transform.Translate(velocity);
    }

    void HorizontalCollisions(ref Vector3 velocity)
    {
        float directionX = Mathf.Sign(velocity.x);
        float rayLenght = Mathf.Abs(velocity.x) + skinWidth;

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLenght, collisionMask);

        Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLenght, Color.red);
            if (hit)
            {
                //get angle of surface hit
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                //if within steepnesslimit climb slope normally
                if(i == 0 && slopeAngle <= maxClimbAngle)
                {
                    if (collisions.descendingSlope)
                    {
                        collisions.descendingSlope = false;
                        velocity = collisions.velocityOld;
                    }
                    float distanceToSlopeStart = 0;
                    if (slopeAngle != collisions.slopeAngleOld)
                    {
                        distanceToSlopeStart = hit.distance - skinWidth;
                        velocity.x -= distanceToSlopeStart * directionX;
                    }
                    ClimbSlope(ref velocity, slopeAngle);
                    velocity.x += distanceToSlopeStart * directionX;
                }
                //only check when not already climbing or when slope steep
                if(!collisions.climbingSlope || slopeAngle > maxClimbAngle)
                {
                    velocity.x = (hit.distance - skinWidth) * directionX;
                    rayLenght = hit.distance;

                    if (collisions.climbingSlope)
                    {
                        velocity.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad * Mathf.Abs(velocity.x));
                    }

                    collisions.left = directionX == -1;
                    collisions.right = directionX == 1;
                }
            }

        }

    }
    void VerticalCollisions(ref Vector3 velocity)
    {
        float directionY = Mathf.Sign(velocity.y);
        float rayLenght = Mathf.Abs(velocity.y) + skinWidth;

        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + velocity.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLenght, collisionMask);

            if (hit)
            {
                velocity.y = (hit.distance - skinWidth) * directionY;
                rayLenght = hit.distance;

                if (collisions.climbingSlope)
                {
                    velocity.x = velocity.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad * Mathf.Sign(velocity.x));
                }

                collisions.below = directionY == -1;
                collisions.above = directionY == 1;
            }

            Debug.DrawRay(rayOrigin * verticalRaySpacing * i, Vector2.up * directionY * rayLenght, Color.red);
        }
        if (collisions.climbingSlope)
        {
            float directionx = Mathf.Sign(velocity.x);
            rayLenght = Mathf.Abs(velocity.x + skinWidth);
            Vector2 rayOrigin = ((directionx == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * velocity.y;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionx, rayLenght, collisionMask);
            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if(slopeAngle != collisions.slopeAngle)
                {
                    //colliding with new slope set vel x
                    velocity.x = (hit.distance - skinWidth) * directionx;
                    collisions.slopeAngle = slopeAngle;
                }
            }
        }
    }

    void ClimbSlope (ref Vector3 velocity, float slopeAngle)
    {
        float moveDistance = Mathf.Abs(velocity.x);
        float climbVelocityY = Mathf.Sin(Mathf.Deg2Rad * slopeAngle) * moveDistance;

        if (velocity.y <= climbVelocityY)
        { 
            velocity.y = climbVelocityY;
            velocity.x = Mathf.Cos(Mathf.Deg2Rad * slopeAngle) * moveDistance * Mathf.Sign(velocity.x);
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
        }
    }

    void DescendSlope(ref Vector3 velocity)
    {
        float directionX = Mathf.Sign(velocity.x);
        
        //cast ray down from leading corner
        Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);
        if (hit)
        {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if (slopeAngle != 0 && slopeAngle <= maxDescendAngle)
            {
                if (Mathf.Sign(hit.normal.x) == directionX)
                {
                    //we are heading down slope normal of slope matches our direction
                    if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x))
                    {
                        //if we are close enough to the slope for it to have effect
                        float moveDistance = Mathf.Abs(velocity.x);
                        float descendVelocityY = Mathf.Sin(Mathf.Deg2Rad * slopeAngle) * moveDistance;
                        velocity.x = Mathf.Cos(Mathf.Deg2Rad * slopeAngle) * moveDistance * Mathf.Sign(velocity.x);
                        velocity.y -= descendVelocityY;
                        collisions.slopeAngle = slopeAngle;
                        collisions.descendingSlope = true;
                        collisions.below = true;
                    } 
                }
            }
        }
    }

    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;
        public bool climbingSlope;
        public float slopeAngle, slopeAngleOld;
        public bool descendingSlope;
        public Vector3 velocityOld;

        public void Reset()
        {
            above = below = false;
            left = right = false;
            climbingSlope = false;
            descendingSlope = false;
            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
        }
    }
}