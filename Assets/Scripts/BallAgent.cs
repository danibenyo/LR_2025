using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

[RequireComponent(typeof(Rigidbody))]
public class BallAgent : Agent
{
    [Header("References")]
    public Transform target;

    Rigidbody rb;
    Vector3 startPosBall;
    Vector3 startPosTarget;

    [Header("Movement")]
    public float moveForce = 10f;

    [Header("Rewards")]
    public bool useDistanceShaping = true;
    public bool useTimePenalty = true;
    public bool useVelocityReward = false;

    [Tooltip("Given when reaching the target.")]
    public float successReward = 1f;

    [Tooltip("Given when falling off platform.")]
    public float fallPenalty = -1f;

    [Tooltip("Distance threshold for completing the task.")]
    public float successDistance = 0.3f;

    [Tooltip("Velocity factor used for optional velocity reward.")]
    public float velocityMultiplier = 0.001f;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        startPosBall = transform.position;

        if (target != null)
            startPosTarget = target.position;
    }

    public override void OnEpisodeBegin()
    {
        // Reset physics
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Reset transforms to exact starting positions
        transform.position = startPosBall;
        transform.rotation = Quaternion.identity;

        if (target != null)
        {
            target.position = startPosTarget;
            target.rotation = Quaternion.identity;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Ball position & velocity
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(rb.linearVelocity);

        // Target position
        sensor.AddObservation(target.localPosition);

        // Direction vector to target
        sensor.AddObservation(target.localPosition - transform.localPosition);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float x = actions.ContinuousActions[0];
        float z = actions.ContinuousActions[1];

        // Apply force
        rb.AddForce(new Vector3(x, 0f, z) * moveForce);

        // ---- REWARD OPTIONS ----

        // 1. Time penalty
        if (useTimePenalty)
            AddReward(-0.001f);

        // 2. Distance shaping (encourages getting closer)
        if (useDistanceShaping)
        {
            float dist = Vector3.Distance(transform.localPosition, target.localPosition);
            float shapedReward = (1f - Mathf.Clamp01(dist / 10f)) * 0.001f;
            AddReward(shapedReward);
        }

        // 3. Velocity bonus (optional)
        if (useVelocityReward)
        {
            float speed = rb.linearVelocity.magnitude;
            AddReward(speed * velocityMultiplier);
        }

        // ---- SUCCESS / FAIL ----

        // Success
        if (Vector3.Distance(transform.localPosition, target.localPosition) < successDistance)
        {
            AddReward(successReward);
            EndEpisode();
        }

        // Fall off
        if (transform.localPosition.y < -1f)
        {
            AddReward(fallPenalty);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var c = actionsOut.ContinuousActions;

        c[0] = Input.GetAxis("Horizontal");
        c[1] = Input.GetAxis("Vertical");
    }
}
