/*
using UnityEngine;
using UnityEditor;
using System.Collections;

public class JellyObj : MonoBehaviour {
	const string rootPath = "D:/zz_test_evo/";

	void Reset () {
		Debug.Log("START");
		UpdateMe();
	}

	private static void DestroyChildren(GameObject go, GameObject butIgnore = null) {
		while (go.transform.childCount > 0) {
			DestroyChildren(go.transform.GetChild(0).gameObject, butIgnore);
		}
		if (go != butIgnore) {
			DestroyImmediate(go);
		}
	}

	void UpdateMe() {
		DestroyChildren(gameObject, gameObject);
		DestroyImmediate(GetComponent<Animation>());

		var mesh1 = Resources.LoadAssetAtPath("Assets/ROSEChars/JELLYBEAN_1.asset", typeof(Mesh)) as Mesh;
		var mesh2 = Resources.LoadAssetAtPath("Assets/ROSEChars/JELLYBEAN_2.asset", typeof(Mesh)) as Mesh;
		var mat1 = Resources.LoadAssetAtPath("Assets/ROSEChars/JELLYBEAN_MAT.mat", typeof(Material)) as Material;

		var go1 = new GameObject();
		go1.name = "Mesh_1";
		go1.transform.parent = transform;
		go1.transform.localPosition = Vector3.zero;
		go1.transform.localRotation = Quaternion.identity;
		go1.transform.localScale = Vector3.one;
		var mr1 = go1.AddComponent<SkinnedMeshRenderer>();
		mr1.sharedMesh = mesh1;
		mr1.material = mat1;
		//go1.active = false;

		var go2 = new GameObject();
		go2.name = "Mesh_2";
		go2.transform.parent = transform;
		go2.transform.localPosition = Vector3.zero;
		go2.transform.localRotation = Quaternion.identity;
		go2.transform.localScale = Vector3.one;
		var mr2 = go2.AddComponent<SkinnedMeshRenderer>();
		mr2.sharedMesh = mesh2;
		mr2.material = mat1;
		//go2.active = false;

		var bf = new Revise.Files.ZMD.BoneFile();
		bf.Load(rootPath + "3DDATA/NPC/PLANT/JELLYBEAN1/JELLYBEAN2_BONE.ZMD");

		var bgos = new GameObject[bf.Bones.Count];
		var bts = new Transform[bf.Bones.Count];
		var mats = new Matrix4x4[bf.Bones.Count];
		var bps = new Matrix4x4[bf.Bones.Count];
		var fname = new string[bf.Bones.Count];
		for (int i = 0; i < bf.Bones.Count; ++i) {
			var b = bf.Bones[i];

			var go = new GameObject();
			//go.name = "Bone_" + i.ToString();
			go.name = b.Name;

			Matrix4x4 myMat = Matrix4x4.TRS(
					rtuPosition(b.Translation) / 100,
					rtuRotation(b.Rotation),
					Vector3.one);
			if (i == 0) {
				go.transform.parent = transform;
				fname[i] = go.name;
				mats[i] = myMat;
			} else {
				go.transform.parent = bgos[b.Parent].transform;
				fname[i] = fname[b.Parent] + "/" + go.name;
				mats[i] = mats[b.Parent] * myMat;
			}
			bps[i] = mats[i].inverse;

			go.transform.localPosition = rtuPosition(b.Translation) / 100;
			go.transform.localRotation = rtuRotation(b.Rotation);
			go.transform.localScale = Vector3.one;

			// For Seeing it...
			go.AddComponent<BoneDebug>();

			//bps[i] = go.transform.worldToLocalMatrix * transform.localToWorldMatrix;

			bgos[i] = go;
			bts[i] = go.transform;
		}

		mr1.sharedMesh.bindposes = bps;
		mr1.bones = bts;

		mr2.sharedMesh.bindposes = bps;
		mr2.bones = bts;

		var zmo = new Revise.Files.ZMO.MotionFile();
		zmo.Load(rootPath + "3DDATA/MOTION/NPC/JELLYBEAN1/JELLYBEAN1_WALK.ZMO");

		var clip = new AnimationClip();

		for (var i = 0; i < zmo.ChannelCount; ++i) {
			if (zmo[i].Index < 0 || zmo[i].Index >= fname.Length) {
				Debug.LogWarning("Found invalid channel index.");
				continue;
			}

			string cbn = fname[zmo[i].Index];
			if (zmo[i].Type == Revise.Files.ZMO.ChannelType.Rotation) {
				var c = zmo[i] as Revise.Files.ZMO.Channels.RotationChannel;
				var curvex = new AnimationCurve();
				var curvey = new AnimationCurve();
				var curvez = new AnimationCurve();
				var curvew = new AnimationCurve();
				for (var j = 0; j < zmo.FrameCount; ++j) {
					var frame = rtuRotation(c.Frames[j]);
					curvex.AddKey((float)j / (float)zmo.FramesPerSecond, frame.x);
					curvey.AddKey((float)j / (float)zmo.FramesPerSecond, frame.y);
					curvez.AddKey((float)j / (float)zmo.FramesPerSecond, frame.z);
					curvew.AddKey((float)j / (float)zmo.FramesPerSecond, frame.w);
				}
				clip.SetCurve(cbn, typeof(Transform), "localRotation.x", curvex);
				clip.SetCurve(cbn, typeof(Transform), "localRotation.y", curvey);
				clip.SetCurve(cbn, typeof(Transform), "localRotation.z", curvez);
				clip.SetCurve(cbn, typeof(Transform), "localRotation.w", curvew);
			} else if (zmo[i].Type == Revise.Files.ZMO.ChannelType.Position) {
				var c = zmo[i] as Revise.Files.ZMO.Channels.PositionChannel;
				var curvex = new AnimationCurve();
				var curvey = new AnimationCurve();
				var curvez = new AnimationCurve();
				for (var j = 0; j < zmo.FrameCount; ++j) {
					var frame = rtuPosition(c.Frames[j]) / 100;
					curvex.AddKey((float)j / (float)zmo.FramesPerSecond, frame.x);
					curvey.AddKey((float)j / (float)zmo.FramesPerSecond, frame.y);
					curvez.AddKey((float)j / (float)zmo.FramesPerSecond, frame.z);
				}
				clip.SetCurve(cbn, typeof(Transform), "localPosition.x", curvex);
				clip.SetCurve(cbn, typeof(Transform), "localPosition.y", curvey);
				clip.SetCurve(cbn, typeof(Transform), "localPosition.z", curvez);
			}
		}
		AssetDatabase.CreateAsset(clip, "Assets/ROSEChars/TestClip.asset");

		var a = gameObject.AddComponent<Animation>();
		a.clip = clip;
		a.wrapMode = WrapMode.Loop;
	}

	static Vector3 rtuPosition(Vector3 v) {
		return new Vector3(v.x, v.z, v.y);
	}
	static Quaternion rtuRotation(Quaternion q) {
		Vector3 v;
		float a;
		q.ToAngleAxis(out a, out v);
		return Quaternion.AngleAxis(-a, new Vector3(v.x, v.z, v.y));
	}
}
*/