using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

//[CustomEditor(typeof(RoseMapObjectData))]
public class RoseMapObjectDataEditor : Editor
{
	public override void OnInspectorGUI()
	{
		var myTarget = (RoseMapObjectData)target;
		var subObjs = myTarget.subObjects;

		int numSubObj = EditorGUILayout.IntField("Mesh Count", subObjs.Count);
		if (numSubObj != subObjs.Count) {
			var newSubObjs = new List<RoseMapObjectData.SubObject>();
			for (var i = 0; i < numSubObj; ++i) {
				if (i <  System.Math.Min(numSubObj, subObjs.Count)) {
					newSubObjs.Add(subObjs[i]);
				} else {
					newSubObjs.Add(new RoseMapObjectData.SubObject());
				}
			}
			myTarget.subObjects = newSubObjs;
			subObjs = newSubObjs;
		}

		for (int i = 0; i < subObjs.Count; ++i) {
			EditorGUILayout.LabelField("Mesh " + i.ToString());
			EditorGUI.indentLevel++;
			subObjs[i].mesh = (Mesh)EditorGUILayout.ObjectField("Mesh", subObjs[i].mesh, typeof(Mesh), false);
			subObjs[i].material = (Material)EditorGUILayout.ObjectField("Material", subObjs[i].material, typeof(Material), false);
			subObjs[i].parent = EditorGUILayout.IntField("Parent", subObjs[i].parent);
			subObjs[i].position = EditorGUILayout.Vector3Field("Position", subObjs[i].position);
			subObjs[i].rotation = Quaternion.Euler(EditorGUILayout.Vector3Field("Rotation", subObjs[i].rotation.eulerAngles));
			subObjs[i].scale = EditorGUILayout.Vector3Field("Scale", subObjs[i].scale);
			EditorGUI.indentLevel--;
		}

		if (GUI.changed) {
			EditorUtility.SetDirty(target);
		}
	}
}