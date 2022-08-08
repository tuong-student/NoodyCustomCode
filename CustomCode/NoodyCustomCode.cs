using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System;
using System.Threading;
using UnityEngine;

namespace NOOD{
    public static class NoodyCustomCode
    {
        public static Thread newThread;

        #region Look, mouse and Vector zone
        public static Vector3 ScreenPointToWorldPoint(Vector2 screenPoint){
            Camera cam = UnityEngine.GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
            return cam.ScreenToWorldPoint(screenPoint);
        }

        public static Vector3 MouseToWorldPoint(){
            Camera cam = UnityEngine.GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
            Vector3 mousePos = Input.mousePosition;
            return cam.ScreenToWorldPoint(mousePos);
        }

        public static Vector3 MouseToWorldPoint2D(){
            Camera cam = UnityEngine.GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
            Vector3 mousePos = MouseToWorldPoint();
            Vector3 temp = new Vector3(mousePos.x, mousePos.y);
            return temp;
        }

        public static Vector2 WorldPointToScreenPoint(Vector3 worldPoint){
            Camera cam = UnityEngine.GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
            return cam.WorldToScreenPoint(worldPoint);
        }

        public static void LookToMouse2D(Transform objectTransform){
            Vector3 mousePosition = MouseToWorldPoint();
            LookToPoint2D(objectTransform, mousePosition);
        }

        public static void LookToPoint2D(Transform objectTransform, Vector3 targetPosition){
            Vector3 lookDirection = LookDirection(objectTransform.position, targetPosition);
            float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
            objectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
        }

        public static Vector3 LookDirection(Vector3 FromPosition, Vector3 targetPosition){
            return (targetPosition - FromPosition).normalized;
        }

        public static Vector3 GetPointAroundAPosition2D(Vector3 centerPosition, float degrees, float radius){
            var radians = degrees * Mathf.Deg2Rad;
            var x = Mathf.Cos(radians);
            var y = Mathf.Sin(radians);
            Vector3 pos = new Vector3(x, y, centerPosition.z);
            return pos += centerPosition;
        }

        public static Vector3 GetPointAroundAPosition2D(Vector3 centerPosition, float radius){
            var degrees = UnityEngine.Random.Range(0, 360);
            var radians = degrees * Mathf.Deg2Rad;
            var x = Mathf.Cos(radians);
            var y = Mathf.Sin(radians);
            Vector3 pos = new Vector3(x, y, centerPosition.z);
            pos *= radius;
            return pos += centerPosition;
        }

        public static Vector3 GetPointAroundAPosition3D(Vector3 centerPosition, float degrees, float radius){
            var radians = degrees * Mathf.Deg2Rad;
            var x = Mathf.Cos(radians);
            var z = Mathf.Sin(radians);
            Vector3 pos = new Vector3(x, centerPosition.y, z);
            return pos += centerPosition;
        }

        public static Vector3 GetPointAroundAPosition3D(Vector3 centerPosition, float radius){
            var degrees = UnityEngine.Random.Range(0, 360);
            var radians = degrees * Mathf.Deg2Rad;
            var x = Mathf.Cos(radians);
            var z = Mathf.Sin(radians);
            Vector3 pos = new Vector3(x, centerPosition.y, z);
            pos *= radius;
            return pos += centerPosition;
        }
        #endregion

        #region Background Function
        public static void RunInBackground(Action function, Queue<Action> mainThreadActions = null){
            //! WebGL doesn't do multithreading

            /* Create a mainThreadQueue in main script to hold the Action and run the action in Update like below
                 if your Action do something to Unity object like set transform, set physic or contain Unity Time class 
                !Ex for mainThreadQueue:
                    Queue<Action> mainThreadQueue = new Queue<Action>()
                    
                    void Update()
                    {
                        if(mainThreadQueue.Count > 0)
                        {
                            Action action = mainThreadQueue.Dequeue();
                            action();
                        }
                    }
            */

            //! if your function has parameters, use param like this
            //! NoodyCustomCode.RunInBackGround(() => yourFunction(parameters)); 

            Thread t = new Thread(() => {
                if(mainThreadActions != null)
                    NoodyCustomCode.AddToMainThread(function, mainThreadActions);
                else
                    function();
            });
            t.Start();
        }

        static void AddToMainThread(Action function, Queue<Action> mainThreadActions){
            mainThreadActions.Enqueue(function);
        }

        //TODO: learn Unity.Jobs and create a Function to run many complex job in multithread

        #endregion
   
        #region Camera
        /// <summary>
        /// Make camera size always show all object with collider (2D and 3D)
        /// (center, size) = CalculateOrthoCamsize();
        /// </summary>
        /// <param name="_camera">Main camera</param>
        /// <param name="_buffer">Amount of padding size</param>
        /// <returns></returns>
        public static (Vector3 center, float size) CalculateOrthoCamSize(Camera _camera, float _buffer)
        {
            var bound = new Bounds(); //Create bound with center Vector3.zero;

            foreach (var col in GameObject.FindObjectsOfType<Collider2D>()) bound.Encapsulate(col.bounds);
            foreach (var col in GameObject.FindObjectsOfType<Collider>()) bound.Encapsulate(col.bounds);

            bound.Expand(_buffer);

            var vertical = bound.size.y;
            var horizontal = bound.size.x * _camera.pixelHeight / _camera.pixelWidth;

            Debug.Log("V: " + vertical + ", H: " + horizontal);

            var size = Mathf.Max(horizontal, vertical) * 0.5f;
            var center = bound.center + new Vector3(0f, 0f, -10f);

            return (center, size);
        } 
            
        public static void SmoothCameraFollow(UnityEngine.GameObject camera, float smoothTime, Transform targetTransform, Vector3 offset, 
        float maxX, float maxY, float minX, float minY){

            Vector3 temp = camera.transform.position;
            Vector3 targetPosition = targetTransform.position + offset;
            Vector3 currentSpeed = Vector3.zero;
            Vector3 smoothPosition = Vector3.SmoothDamp(camera.transform.position, targetPosition, ref currentSpeed, smoothTime);
            if(smoothPosition.x < maxX && smoothPosition.x > minX)
                temp.x = smoothPosition.x;
            if(smoothPosition.y < maxY && smoothPosition.y > minY)
                temp.y = smoothPosition.y;
            temp.z = smoothPosition.z;
            camera.transform.position = temp;
        }

        public static IEnumerator ObjectShake(UnityEngine.GameObject @object, float duration, float magnitude)
        {
            Vector3 OriginalPos = @object.transform.localPosition;
            float elapsed = 0.0f;
            float range = 1f;
            while (elapsed < duration)
            {
                float x, y;
                if((elapsed / duration) * 100 < 80){
                    //Starting shake
                    x = UnityEngine.Random.Range(-range, range) * magnitude;
                    y = UnityEngine.Random.Range(-range, range) * magnitude;
                }
                else{
                    //Ending
                    range -= Time.deltaTime * elapsed;
                    x = UnityEngine.Random.Range(-range, range) * magnitude;
                    y = UnityEngine.Random.Range(-range, range) * magnitude;
                }
                
                @object.transform.localPosition = new Vector3(x, y, OriginalPos.z);
                    
                elapsed += Time.deltaTime;
                yield return null;
            }
            @object.transform.localPosition = OriginalPos;
        }
        #endregion
    
        #region Color
        /// <summary>
        /// <para>Return RGBA color </para>
        /// </summary>
        /// <param name="hexCode">hex code form RRGGBB or RRGGBBAA for alpha output</param>
        /// <returns>Color with RGBA form</returns>
        public static Color HexToColor(string hexCode)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hexCode, out color);

            return color;
        }
        //----------------------------//
        /// <summary>
        /// Return hex code with alpha of the color
        /// </summary>
        /// <param name="color">Color's form RRGGBBAA</param>
        /// <returns></returns>
        public static string ColorAToHex(Color color)
        {
            return ColorUtility.ToHtmlStringRGBA(color);
        }
        //----------------------------//
        /// <summary>
        /// Return hex code without alpha of the color
        /// </summary>
        /// <param name="color">Color's form RRGGBB</param>
        /// <returns></returns>
        public static string ColorToHex(Color color)
        {
            return ColorUtility.ToHtmlStringRGB(color);
        }
        #endregion
    }

}
