using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Char_Phys_Float : MonoBehaviour
{
    [Header("Floating")]
    [SerializeField] [Min(0f)] [Tooltip("The length of the ray")] private float m_RayLength;
    [SerializeField] [Min(0f)] private float m_RideHeight;
    [SerializeField] [Min(0f)] private float m_RideSpringStrength;
    [SerializeField] [Min(0f)] private float m_RideSpringDamper;

    [Header("Self-righting")]
    private Quaternion m_UprightJointTargetRot = Quaternion.identity;
    [SerializeField] [Min(0f)] private float m_UprightJointSpringStrength;
    [SerializeField] [Min(0f)] private float m_UprightJointSpringDamper;

    [Header("Locomaotion")]
    [SerializeField] [Min(0f)] private float m_MaxSpeed = 8f;
    [SerializeField] [Min(0f)] private float m_Acceleration = 200f;
    [SerializeField] [Min(0f)] private float m_MaxAccelForce = 150f;
    [SerializeField] [Min(0f)] private AnimationCurve m_AccelerationFactorFromDot;
    [SerializeField] [Min(0f)] private AnimationCurve m_MaxAccelerationFactorFromDot;

    [Header("Jumping")]
    [SerializeField] [Min(0f)] private float m_JumpingForce = 7.5f;
    [SerializeField] [Min(0f)] private float m_CoyoteDuration = 0.33f;
    private float m_CoyoteTimer;
    [SerializeField] [Min(0f)] private float m_JumpBufferDuration = 0.33f;
    private float m_JumpBufferTimer;
    [SerializeField] [Min(0f)] private float m_JumpSkipGroundCheckDuration = 0.5f;
    private float m_JumpSkipGroundCheckTimer;
    [SerializeField] [Min(0f)] private float m_JumpDuration = 0.667f;
    private float m_JumpTimer;
    [SerializeField] [Min(0f)] private AnimationCurve m_AnalogueJumpUpForce;
    private bool m_Grounded;

    [Header("References")]
    [SerializeField] private Transform m_CameraDolly;
    private Vector3 m_InputMove;
    private Vector3 m_UnitGoal;
    private Vector3 m_GoalVel;
    private Rigidbody m_RB;

    private void Awake()
    {
        m_RB = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        m_InputMove = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        m_UnitGoal = m_CameraDolly.TransformDirection(m_InputMove); 

        if(Mathf.Abs(m_RB.velocity.x) > 0.1f || Mathf.Abs(m_RB.velocity.z) > 0.1f)
        {
            m_UprightJointTargetRot = Quaternion.LookRotation(new Vector3(m_RB.velocity.x, 0f, m_RB.velocity.z).normalized, Vector3.up);
        }

        //Coyote Time
        if(!m_Grounded)
        {
            if(m_CoyoteTimer > 0f)
            {
                m_CoyoteTimer -= Time.deltaTime;
            }
        }
        else
        {
            if(m_CoyoteTimer != m_CoyoteDuration)
            {
                m_CoyoteTimer = m_CoyoteDuration;
            }

            if(m_JumpTimer != 0f)
            {
                m_JumpTimer = 0f;
            }
        }

        //Jump Buffering
        if(Input.GetButtonDown("Jump"))
        {
            m_JumpBufferTimer = m_JumpBufferDuration;
        }
        else if(m_JumpBufferTimer > 0f)
        {
            m_JumpBufferTimer -= Time.deltaTime;
        }
    }

    void FixedUpdate()
    {
        RaycastHit outHit;
        if (m_JumpSkipGroundCheckTimer > 0f)
        {
            m_JumpSkipGroundCheckTimer -= Time.fixedDeltaTime;
        }
        else
        {
            m_Grounded = true;
            if (Physics.Raycast(transform.position, Vector3.down, out outHit, m_RayLength))
            {
                //Float Spring
                Vector3 vel = m_RB.velocity;
                Vector3 rayDir = Vector3.down;
                Vector3 otherVel = Vector3.zero;
                Rigidbody hitBody = outHit.rigidbody;
                if (hitBody != null)
                {
                    otherVel = hitBody.velocity;
                }

                float rayDirVel = Vector3.Dot(rayDir, vel);
                float otherDirVel = Vector3.Dot(rayDir, otherVel);

                float relVel = rayDirVel - otherDirVel;

                float x = outHit.distance - m_RideHeight;

                float springForce = (x * m_RideSpringStrength) - (relVel * m_RideSpringDamper);

                Debug.DrawLine(transform.position, transform.position + (rayDir * springForce), Color.yellow);

                m_RB.AddForce(rayDir * springForce, ForceMode.Force);

                if (hitBody != null)
                {
                    hitBody.AddForceAtPosition(rayDir * -springForce, outHit.point, ForceMode.Force);
                }
            }
            else
            {
                m_Grounded = false;
            }
        }

        Quaternion charCurrent = transform.rotation;
        Quaternion toGoal = m_UprightJointTargetRot * Quaternion.Inverse(charCurrent);

        Vector3 rotAxis;
        float rotDegrees;

        toGoal.ToAngleAxis(out rotDegrees, out rotAxis);
        rotAxis.Normalize();
        rotDegrees -= (rotDegrees > 180f) ? 360f : 0f;

        float rotRadians = rotDegrees * Mathf.Deg2Rad;
        if (!float.IsNaN(rotAxis.x))
        {
            m_RB.AddTorque((rotAxis * (rotRadians * m_UprightJointSpringStrength)) - (m_RB.angularVelocity * m_UprightJointSpringDamper));
        }

        ApplyMovement();

        //Jumping
        if(m_CoyoteTimer > 0f && m_JumpBufferTimer > 0f)
        {
            m_RB.velocity = new Vector3(m_RB.velocity.x, 0f, m_RB.velocity.z);
            m_JumpSkipGroundCheckTimer = m_JumpSkipGroundCheckDuration;
            m_JumpTimer = m_JumpDuration;
            m_Grounded = false;
            m_JumpBufferTimer = 0f;
        }

        if(m_JumpTimer > 0f)
        {
            m_JumpTimer -= Time.fixedDeltaTime;
            if(Input.GetButton("Jump"))
            {
                m_RB.AddForce(Vector3.up * m_JumpingForce * m_AnalogueJumpUpForce.Evaluate(m_JumpTimer / m_JumpDuration), ForceMode.Force);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color32(255, 0, 0, 150);
        Gizmos.DrawCube(transform.position + (Vector3.down * m_RayLength * 0.5f), new Vector3(0.1f, m_RayLength, 0.1f));
        Gizmos.color = new Color32(255, 120, 20, 150);
        Gizmos.DrawCube(transform.position + (Vector3.down * m_RideHeight), new Vector3(0.2f, 0.05f, 0.2f));
    }

    private void ApplyMovement()
    {
        //Calculate the goal velocity
        Vector3 unitVel = m_GoalVel.normalized;

        float velDot = Vector3.Dot(m_UnitGoal, unitVel);

        float accel = m_Acceleration * m_AccelerationFactorFromDot.Evaluate(velDot);

        Vector3 goalVel = m_UnitGoal * m_MaxSpeed;

        m_GoalVel = Vector3.MoveTowards(m_GoalVel, goalVel, accel * Time.fixedDeltaTime);

        //Actual Force
        Vector3 neededAccel = (m_GoalVel - new Vector3(m_RB.velocity.x, 0f, m_RB.velocity.z)) / Time.fixedDeltaTime;

        float maxAccel = m_MaxAccelForce * m_MaxAccelerationFactorFromDot.Evaluate(velDot);

        neededAccel = Vector3.ClampMagnitude(neededAccel, maxAccel);

        m_RB.AddForce(neededAccel, ForceMode.Force);
    }
}
