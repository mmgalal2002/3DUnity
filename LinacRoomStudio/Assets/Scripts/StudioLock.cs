using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoomStudio {
public partial class StudioApp {
 void SelectionLockCheckbox(IList<Item> selectedItems){
  if(selectedItems.Count==0)return;
  var ids=SelectionEditing.Expand(design,selection);
  var members=design.items.Where(item=>ids.Contains(item.id)).ToList();
  bool allLocked=members.All(item=>item.locked);
  bool mixed=members.Any(item=>item.locked)&&!allLocked;
  bool changedBefore=GUI.changed;
  bool next=GUILayout.Toggle(allLocked," Lock in place (no movement or editing)");
  bool unlockMixed=false;
  if(mixed){
   GUILayout.Label("Some selected objects are locked.",small);
   unlockMixed=Btn("Unlock all selected objects");
  }
  if(next!=allLocked||unlockMixed){
   if(dirty)Commit();
   SelectionEditing.SetLock(design,selection,next);
   numberBuffers.Clear();Commit();Rebuild();
   status=next?"Selected objects locked in place.":"Selected objects unlocked.";
  }
  GUI.changed=changedBefore;
 }
}
}
