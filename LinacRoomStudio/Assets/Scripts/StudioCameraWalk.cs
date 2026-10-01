using UnityEngine;

namespace RoomStudio {
public partial class StudioApp {
 Vector3 cameraWalkVelocity;
 bool cameraTextFocused;

 void UpdateCameraWalk(){
  if(top||cameraTextFocused||showHelp||showFiles||showQa||showComponentEditor||showPlanAuthoring||showReset||dragging||resizingWall||scalingEquipment||marquee||
     Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl)||Input.GetKey(KeyCode.LeftAlt)||Input.GetKey(KeyCode.RightAlt)){
   cameraWalkVelocity=Vector3.zero;
   return;
  }
  float sideways=(Input.GetKey(KeyCode.RightArrow)?1f:0f)-(Input.GetKey(KeyCode.LeftArrow)?1f:0f);
  float forward=(Input.GetKey(KeyCode.UpArrow)?1f:0f)-(Input.GetKey(KeyCode.DownArrow)?1f:0f);
  StepCameraWalk(sideways,forward,Mathf.Min(Time.unscaledDeltaTime,.05f));
 }

 void StepCameraWalk(float sideways,float forward,float deltaTime){
  Vector3 direction=Quaternion.Euler(0,yaw,0)*new Vector3(sideways,0,forward);
  if(direction.sqrMagnitude>1f)direction.Normalize();
  float speed=Mathf.Clamp(zoom*.2f,2.5f,8f);
  cameraWalkVelocity=Vector3.MoveTowards(cameraWalkVelocity,direction*speed,(direction.sqrMagnitude>0f?12f:16f)*deltaTime);
  focus+=cameraWalkVelocity*deltaTime;
 }
}
}
