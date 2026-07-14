// using UnityEngine;

// /// <summary>
// /// PURPOSE:
// /// Marks the Case Folder object as a valid stamping destination, per design
// /// doc: "The player drops the stamp onto the Case Folder." This is a SEPARATE
// /// component from CaseFolderInteractable (which still handles opening the
// /// folder UI as normal) — this one only matters while a stamp is currently
// /// selected in StampUI.
// ///
// /// WHY A SEPARATE COMPONENT RATHER THAN MODIFYING CaseFolderInteractable:
// /// CaseFolderInteractable's OnInteract() already has a clear, single job
// /// (open the folder). Overloading it with "...unless a stamp is selected,
// /// in which case do something totally different" would violate single
// /// responsibility. Two independent IInteractable components can coexist on
// /// the same GameObject cleanly — WorkstationInteractor's raycast will hit
// /// whichever IInteractable it finds via GetComponent, so see the Editor
// /// setup note below on how selection priority is resolved.
// ///
// /// CONNECTS WITH:
// /// - StampUI: notified when the folder is clicked while a stamp is held
// /// </summary>
// public class StampTargetInteractable : MonoBehaviour
// {
//     [SerializeField] private StampUI stampUI;

//     /// <summary>
//     /// Called directly by StampUI's own click-forwarding (see StampUI below)
//     /// rather than being a full IInteractable itself — this avoids the
//     /// "two IInteractable components on one GameObject" ambiguity entirely.
//     /// </summary>
//     public void ReceiveStamp()
//     {
//         stampUI.OnFolderStamped();
//     }
// }