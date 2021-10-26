using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_Chase : MonoBehaviour
{
    [SerializeField] private Vector3 m_LocalCamOffset;
    [SerializeField] private Transform m_TrackedTransform;
    [Tooltip("This should never go above the framerate of the camera will become choppy")]
    [SerializeField] private float m_CamSpeed = 3f;
    [SerializeField] private float m_Sensitivity = 8f;

    private void LateUpdate()
    {
        //intent


        //auto cam
        Vector3 toTarget = m_TrackedTransform.position - transform.position;

        transform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);

        float worldYRotRad = transform.rotation.eulerAngles.y * Mathf.Deg2Rad;

        Vector3 worldOrbitPoint = transform.position - new Vector3(m_LocalCamOffset.z * Mathf.Sin(worldYRotRad) + m_LocalCamOffset.x * Mathf.Cos(worldYRotRad), 
                                                                   m_LocalCamOffset.y,
                                                                   m_LocalCamOffset.z * Mathf.Cos(worldYRotRad) - m_LocalCamOffset.x * Mathf.Sin(worldYRotRad));

        Vector3 worldCamOffset = transform.position - worldOrbitPoint;

        float distToTargetOffset = (m_TrackedTransform.position - worldOrbitPoint).magnitude;

        transform.position = Vector3.MoveTowards(worldOrbitPoint, m_TrackedTransform.position, distToTargetOffset * m_CamSpeed * Time.deltaTime);
    }
}
