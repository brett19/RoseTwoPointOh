using UnityEngine;
using UnityEditor;
using System.Collections;

[InitializeOnLoad]
public class RoseEditor : Editor {
	static RoseEditor ()
	{
		SceneView.onSceneGUIDelegate += OnScene;
	}

	private static void OnScene(SceneView sceneview)
	{
		if (Event.current.type == EventType.DragUpdated)
		{
			DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
		}
		else if (Event.current.type == EventType.DragPerform)
		{
			foreach (Object i in DragAndDrop.objectReferences)
			{
				Debug.Log(i as AssetImporter);
				Debug.Log(i as RoseMapObjectData);
				Debug.Log(i.GetType());
				Debug.Log(i.name);
			}
			foreach (string i in DragAndDrop.paths)
			{
				Debug.Log(i);
			}
		}
	}

	/*
	static void ProjectWindowItemOnGUI(string guid, Rect selectionRect)
	{
		if (Event.current.type == EventType.DragUpdated) {
			Debug.Log(Event.current.mousePosition);
			foreach (SceneView i in SceneView.sceneViews)
			{
				Debug.Log(i.position);
				if (i.position.Contains(Event.current.mousePosition))
				{
					Debug.Log("Got Event");
				}
			}
		}
	}
	*/
}
