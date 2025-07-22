using gdio.unreal_api;
using gdio.common.objects;
using System;

namespace LyraGamedriverTests
{
    public static class LyraTestHelpers
    {
        //Aiming at specific point
        public static void AimAtPoint(ApiClient api, string cameraLocator, Vector3 targetPoint)
        {
            Vector3 cameraPos = api.GetObjectPosition(cameraLocator);

            //Calculate vector from the camera to target point
            Vector3 direction = new Vector3(targetPoint.x - cameraPos.x, targetPoint.y - cameraPos.y, targetPoint.z - cameraPos.z);
            float length = (float)Math.Sqrt(direction.x * direction.x + direction.y * direction.y + direction.z * direction.z);
            direction.x /= length;
            direction.y /= length;
            direction.z /= length;

            //Converting Vector to Euler
            //Need to pass rotation as Vector3 so GameDriver can convert it to FRotaror in Engine
            double pitch = -Math.Asin(direction.z) * (180.0 / Math.PI);
            double yaw = Math.Atan2(direction.y, direction.x) * (180.0 / Math.PI);
            Vector3 eulerRotation = new Vector3((float)pitch, (float)yaw, 0);

            //Call custom method and pass eulerRotation in Vector format
            string playerControllerLocator = "//*[contains(@name, 'LyraPlayerController')]";
            api.CallMethod(playerControllerLocator, "SetPlayerViewRotation", new object[] { eulerRotation });
        }
    }
}
