using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mapsui.UI
{
    public enum TouchMode
    {
        None = 0,
        Dragging = 1,
        Zooming = 2
    }

    [Flags]
    public enum MotionEventActions
    {
        Up,
        Down,
        Move,
        Pointer1Down,
        Pointer2Down,
        Pointer3Down,
        Pointer1Up,
        Pointer2Up,
        Pointer3Up,
    }

    public enum TouchStatus
    {
        Up,
        Down,
        Move
    }

    public class TouchEvent
    {
        public int Index;
        public Vector2 Position;
        public TouchStatus Status;
    }

    public class MotionEvent
    {
        private List<TouchEvent> _touches;

        public MotionEventActions Action;
        public int ActionIndex;


        public MotionEvent(params TouchEvent[] touches)
        {
            _touches = new List<TouchEvent>();            
            _touches.AddRange(touches);  
        }

        public int PointerCount
        {
            get
            {
                return _touches.Count;
            }
        }

        
        public float GetX(int motionIndex)
        {
            return _touches[motionIndex].Position.x;
        }

        public float GetY(int motionIndex)
        {
            return _touches[motionIndex].Position.y;
        }
    }
}
