// Indoor/IndoorSwipeListener.cs
// THAY: IndoorMapController.Instance → FloorViewer.Instance

using UnityEngine;
using UnityEngine.EventSystems;

public class IndoorSwipeListener : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Vector2 dragStartPosition;

    public void OnBeginDrag(PointerEventData eventData) => dragStartPosition = eventData.position;
    public void OnDrag(PointerEventData eventData) { }  // Phải giữ trống để EventSystem track đúng

    public void OnEndDrag(PointerEventData eventData)
    {
        if (Input.touchCount > 1) return;
        // Kiểm tra scrollRect từ FloorViewer thay vì IndoorMapController
        if (FloorViewer.Instance?.scrollRect != null && FloorViewer.Instance.scrollRect.enabled) return;  // ĐỔI

        float swipeX = eventData.position.x - dragStartPosition.x;
        if (Mathf.Abs(swipeX) > UIConstants.SwipeThresholdPixels)  // ĐỔI magic number
        {
            if (swipeX < 0) FloorViewer.Instance?.SwitchToAdjacentFloor(1);   // ĐỔI
            else FloorViewer.Instance?.SwitchToAdjacentFloor(-1);  // ĐỔI
        }
    }
}