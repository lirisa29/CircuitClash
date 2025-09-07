using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Movement")]
    public float panSpeed = 20f;
    public float zoomSpeed = 50f;
    public float minZoom = 5f;
    public float maxZoom = 10f;

    [Header("Intro Zoom Settings")]
    public float zoomDuration = 3f;

    private bool introDone = false;
    private CinemachineCamera vCam;

    private void Start()
    {
        vCam = GetComponent<CinemachineCamera>();

        // Set initial max zoom
        var lens = vCam.Lens;
        lens.OrthographicSize = maxZoom;
        vCam.Lens = lens;

        // Start intro zoom
        StartCoroutine(IntroZoom());
    }

    private void Update()
    {
        if (!introDone) return;

        HandlePan();
        HandleZoom();
    }

    private void HandlePan()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        if (Mathf.Abs(moveX) > 0.01f || Mathf.Abs(moveZ) > 0.01f)
        {
            Vector3 move = new Vector3(moveX, 0, moveZ) * panSpeed * Time.deltaTime;
            transform.position += move;
        }
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            var lens = vCam.Lens;
            lens.OrthographicSize = Mathf.Clamp(lens.OrthographicSize - scroll * zoomSpeed * Time.deltaTime, minZoom, maxZoom);
            vCam.Lens = lens;
        }
    }

    private IEnumerator IntroZoom()
    {
        float elapsed = 0f;
        float startZoom = maxZoom;
        float endZoom = minZoom;

        while (elapsed < zoomDuration)
        {
            var lens = vCam.Lens;
            lens.OrthographicSize = Mathf.Lerp(startZoom, endZoom, elapsed / zoomDuration);
            vCam.Lens = lens;

            elapsed += Time.deltaTime;
            yield return null;
        }

        var finalLens = vCam.Lens;
        finalLens.OrthographicSize = endZoom;
        vCam.Lens = finalLens;

        introDone = true;
    }
}
