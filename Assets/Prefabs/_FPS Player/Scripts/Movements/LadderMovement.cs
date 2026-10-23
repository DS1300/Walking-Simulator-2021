using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class LadderMovement : MovementType
{
    [SerializeField]
    private LayerMask ladderLayer;
    public TowerLadderAccess[] towerLadders;
    TowerLadderAccess activeLadder;
    bool enteringFromTop;
    bool steppingOff;
    float nextAttach;
    Vector3 ladderNormal = Vector3.zero;
    Vector3 lastTouch = Vector3.zero;

    public override void Movement()
    {
        if (activeLadder != null)
        {
            MoveOnTowerLadder();
            return;
        }
        Vector3 input = playerInput.input;
        Vector3 move = Vector3.Cross(Vector3.up, ladderNormal).normalized;
        move *= input.x;
        move.y = input.y * movement.walkSpeed;

        bool goToGround = (move.y < -0.02f && movement.grounded);

        if (playerInput.Jump())
        {
            movement.Jump((-ladderNormal + Vector3.up * 2f).normalized, 1f);
            playerInput.ResetJump();
            player.ChangeStatus(Status.walking);
        }

        if (!player.hasObjectInfront(0.05f, ladderLayer) || goToGround)
        {
            player.ChangeStatus(Status.walking);
            Vector3 pushUp = ladderNormal;
            pushUp.y = 0.25f;

            movement.ForceMove(pushUp, movement.walkSpeed, 0.25f, true);
        }
        else
            movement.Move(move, 1f, 0f);
    }

    public override void Check(bool canInteract)
    {
        if (Time.time < nextAttach || PauseMenu.paused) return;
        if (activeLadder != null && player.status == Status.climbingLadder) return;
        activeLadder = null;
        if (canInteract && towerLadders != null)
        {
            foreach (var access in towerLadders)
            {
                if (access == null || !access.enabled) continue;
                Vector3 center = movement.controller.bounds.center;
                bool descend = playerInput.input.y < -.02f && access.AtEntry(center);
                bool ascend = playerInput.input.y > .02f && access.NearFace(center, player.info.radius) &&
                    Vector3.Dot(transform.forward, -access.Outward) > .4f;
                if (!descend && !ascend) continue;
                activeLadder = access;
                enteringFromTop = descend;
                steppingOff = false;
                ladderNormal = -access.Outward;
                player.ChangeStatus(changeTo, IK);
                return;
            }
        }
        //Check for ladder all across player (so they cannot use the side)
        bool right = Physics.Raycast(transform.position + (transform.right * player.info.halfradius), transform.forward, player.info.radius + 0.125f, ladderLayer);
        bool left = Physics.Raycast(transform.position - (transform.right * player.info.halfradius), transform.forward, player.info.radius + 0.125f, ladderLayer);

        if (Physics.Raycast(transform.position, transform.forward, out var hit, player.info.radius + 0.125f, ladderLayer) && right && left)
        {
            if (hit.normal != hit.transform.forward) return;

            ladderNormal = -hit.normal;
            if (player.hasObjectInfront(0.05f, ladderLayer) && playerInput.input.y > 0.02f)
                player.ChangeStatus(changeTo, IK);
        }
    }

    void LeaveTowerLadder()
    {
        activeLadder = null;
        nextAttach = Time.time + .35f;
        player.ChangeStatus(Status.walking);
    }

    void MoveOnTowerLadder()
    {
        if (PauseMenu.paused || Time.timeScale <= 0f) return;
        Vector2 input = playerInput.input;
        Bounds body = movement.controller.bounds;
        if (playerInput.Jump())
        {
            movement.Jump((activeLadder.Outward + Vector3.up * 2f).normalized, 1f);
            playerInput.ResetJump();
            LeaveTowerLadder();
            return;
        }

        float landingY = activeLadder.upperLanding.position.y;
        if (enteringFromTop || steppingOff)
        {
            if (enteringFromTop && Mathf.Abs(input.y) < .02f)
            {
                movement.Move(Vector3.zero, 1f, 0f);
                return;
            }
            Vector3 target = steppingOff ? activeLadder.upperLanding.position :
                activeLadder.FacePoint(body.center) + activeLadder.Outward * (player.info.radius + .06f);
            target.y = landingY + body.extents.y + .06f;
            Vector3 delta = target - body.center;
            float speed = Mathf.Min(movement.walkSpeed, delta.magnitude / Time.fixedDeltaTime);
            movement.Move(delta.normalized * speed, 1f, 0f);
            if (delta.magnitude < .06f)
            {
                if (steppingOff) LeaveTowerLadder();
                else enteringFromTop = false;
            }
            return;
        }

        if (input.y >= -.02f && body.min.y >= landingY + .03f)
        {
            steppingOff = true;
            movement.Move(Vector3.zero, 1f, 0f);
            return;
        }
        if (!activeLadder.NearFace(body.center, player.info.radius))
        {
            LeaveTowerLadder();
            return;
        }
        if (input.y < -.02f && movement.grounded &&
            body.min.y < activeLadder.ladder.bounds.min.y + .3f)
        {
            LeaveTowerLadder();
            return;
        }

        // Follow the ladder plane, including its incline, rather than the camera direction.
        Vector3 outward = activeLadder.Outward;
        Vector3 up = Vector3.ProjectOnPlane(Vector3.up, outward).normalized;
        Vector3 side = Vector3.Cross(Vector3.up, -outward).normalized;
        Vector3 move = up * (input.y * movement.walkSpeed) + side * input.x;
        float gap = Vector3.Dot(body.center - activeLadder.FacePoint(body.center), outward);
        float correction = Mathf.Clamp((player.info.radius + .06f - gap) / Time.fixedDeltaTime,
            -activeLadder.alignmentSpeed, activeLadder.alignmentSpeed);
        move += outward * correction;
        movement.Move(move, 1f, 0f);
    }

    public override IKData IK()
    {
        IKData data = new IKData();
        Vector3 upOffset = Vector3.up * player.info.radius * 2f;
        Vector3 handUp = Vector3.Cross(ladderNormal, Vector3.up);
        if (Physics.SphereCast(transform.position + upOffset, player.info.radius, ladderNormal, out var hit, 0.125f, ladderLayer))
        {
            if (Physics.SphereCast(hit.point + handUp, 0.125f, -handUp, out var hit2, 1.125f, ladderLayer))
                lastTouch = hit2.point - (ladderNormal * 0.125f);
        }
        lastTouch.y = (int)(lastTouch.y * 2f) / 2f;
        data.handPos = lastTouch;

        data.handEulerAngles = Quaternion.LookRotation(ladderNormal, handUp).eulerAngles;
        data.armElbowPos = transform.position + handUp * player.info.radius;
        data.armLocalPos.x = -0.35f;
        return data;
    }
}
