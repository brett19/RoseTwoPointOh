using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

public class Main {
	[MenuItem("ROSE/A Test")]
	static void Test() {
		try {
			AssetDatabase.StartAssetEditing();
			ImportCharModel1();
		} finally {
			AssetDatabase.StopAssetEditing();
		}
		try {
			AssetDatabase.StartAssetEditing();
			ImportCharModel2();
		} finally {
			AssetDatabase.StopAssetEditing();
		}
	}

	[MenuItem("ROSE/Import Model Databases")]
	static void ImportModelDatabases()
	{
		var files = new List<ModelDatabaseImporter>();
		files.Add(new ModelDatabaseImporter("JUNON_JDT_DECO", "3DDATA/JUNON/LIST_DECO_JDT.ZSC"));
		files.Add(new ModelDatabaseImporter("JUNON_JDT_CNST", "3DDATA/JUNON/LIST_CNST_JDT.ZSC"));

		try {
			AssetDatabase.StartAssetEditing();
			foreach (var file in files)
				file.CopyTextures();
		} finally {
			AssetDatabase.StopAssetEditing();
		}

		try {
			AssetDatabase.StartAssetEditing();
			foreach (var file in files)
				file.ImportModels();
		} finally {
			AssetDatabase.StopAssetEditing();
		}
	}

	[MenuItem("ROSE/Import Map")]
	static void DoSomething()
	{
		Debug.Log("Importing...");

		Directory.CreateDirectory("Assets/ROSEPmaps/JUNON/JDT01");

		Terrain[,] ter = new Terrain[65, 65];

		var zon = new Revise.Files.ZON.ZoneFile();
		zon.Load(rootPath + "3DDATA/MAPS/JUNON/JDT01/JDT01.zon");

		ImportMap("JUNON", "JDT01", zon);

		for (int ix = 31; ix <= 34; ++ix)
		{
			for (int iy = 30; iy <= 33; ++iy)
			{
				ter[ix, iy] = ImportTerrain("JUNON", "JDT01", zon, ix, iy);
			}
		}
		/*
		for (int ix = 0; ix < 65; ++ix)
		{
			for (int iy = 0; iy < 65; ++iy)
			{
				if (!ter[ix,iy]) {
					continue;
				}
				Terrain left = null; 
				Terrain top = null;
				Terrain right = null;
				Terrain bottom = null;
				if (ix > 0) left = ter[ix - 1, iy];
				if (iy > 0) top = ter[ix, iy - 1];
				if (ix < 65) right = ter[ix + 1, iy];
				if (iy < 65) bottom = ter[ix, iy + 1];
				ter[ix, iy].SetNeighbors(left, top, right, bottom);
			}
		}
		*/
	}

	const string charBasePath = "Assets/ROSEChars/";
	static public void ImportCharModel1() {
		Directory.CreateDirectory(charBasePath);

		string ddsPath = rootPath + "3DDATA/NPC/PLANT/JELLYBEAN1/BODY02.DDS";
		string texPath = charBasePath + "JELLYBEAN.DDS";
		if (!File.Exists(texPath)) {
			File.Copy(ddsPath, texPath);
			AssetDatabase.ImportAsset(texPath);
		}
	}

	static public void ImportCharMesh(string meshPath, string zmsPath) {
		if (!File.Exists(zmsPath)) {
			Debug.LogWarning("Failed to find referenced ZMS.");
			return;
		}

		var mesh = new Mesh();

		var zms = new Revise.Files.ZMS.ModelFile();
		zms.Load(zmsPath);

		var verts = new Vector3[zms.Vertices.Count];
		var uvs = new Vector2[zms.Vertices.Count];
		var bones = new BoneWeight[zms.Vertices.Count];

		for (int k = 0; k < zms.Vertices.Count; ++k) {
			var v = zms.Vertices[k];
			v.TextureCoordinates[0].y = 1 - v.TextureCoordinates[0].y;

			verts[k] = rtuPosition(v.Position);
			uvs[k] = v.TextureCoordinates[0];

			bones[k] = new BoneWeight();
			bones[k].boneIndex0 = zms.BoneTable[v.BoneIndices.X];
			bones[k].boneIndex1 = zms.BoneTable[v.BoneIndices.Y];
			bones[k].boneIndex2 = zms.BoneTable[v.BoneIndices.Z];
			bones[k].boneIndex3 = zms.BoneTable[v.BoneIndices.W];
			bones[k].weight0 = v.BoneWeights.x;
			bones[k].weight1 = v.BoneWeights.y;
			bones[k].weight2 = v.BoneWeights.z;
			bones[k].weight3 = v.BoneWeights.w;
		}
		mesh.vertices = verts;
		mesh.uv = uvs;
		mesh.boneWeights = bones;

		int[] indices = new int[zms.Indices.Count * 3];
		for (int k = 0; k < zms.Indices.Count; ++k) {
			indices[k * 3 + 0] = zms.Indices[k].X;
			indices[k * 3 + 2] = zms.Indices[k].Y;
			indices[k * 3 + 1] = zms.Indices[k].Z;
		}
		mesh.triangles = indices;

		mesh.RecalculateNormals();

		AssetDatabase.CreateAsset(mesh, meshPath);
	}

	static public void ImportCharModel2() {
		ImportCharMesh(charBasePath + "JELLYBEAN_1.asset", rootPath + "3DDATA/NPC/PLANT/JELLYBEAN1/BODY01.ZMS");
		ImportCharMesh(charBasePath + "JELLYBEAN_2.asset", rootPath + "3DDATA/NPC/PLANT/JELLYBEAN1/BODY02.ZMS");
	}

	static public string hashFile(string filePath) {
		using (var c = new SHA256Managed()) {
			using (var f = new FileStream(filePath, FileMode.Open))
			{
				byte[] hash = c.ComputeHash(f);
				StringBuilder sb = new StringBuilder();
				foreach (byte b in hash) {
					sb.Append(b.ToString("x2"));
				}
				return sb.ToString();
			}
		}
	}

	const string rootPath = "D:/zz_test_evo/";

	private class ModelDatabaseImporter
	{
		private string _name;
		private string _basePath;
		private Revise.Files.ZSC.ModelListFile _f;

		Dictionary<string, int> hashLookup = new Dictionary<string, int>();
		int[] texLookup = null;
		Mesh[] _meshLookup = null;
		Material[] _matLookup = null;
		int texIndex = 0;

		public ModelDatabaseImporter(string name, string zscPath)
		{
			_name = name;
			_basePath = "Assets/ROSEMdls/" + _name + "/";

			Directory.CreateDirectory(_basePath);

			_f = new Revise.Files.ZSC.ModelListFile();
			_f.Load(rootPath + zscPath);

			texLookup = new int[_f.TextureFiles.Count];
		}

		public void CopyTextures()
		{
			for (int i = 0; i < _f.TextureFiles.Count; ++i)
			{
				var ddsPath = rootPath + _f.TextureFiles[i].FilePath;
				if (!File.Exists(ddsPath))
				{
					Debug.LogWarning("Could not find referenced texture - " + ddsPath);
					continue;
				}

				string texHash = hashFile(ddsPath);
				if (hashLookup.ContainsKey(texHash))
				{
					texLookup[i] = hashLookup[texHash];
					continue;
				}

				var texIdx = texIndex++;
				hashLookup[texHash] = texIdx;
				texLookup[i] = texIdx;

				var texPath = _basePath + "Tex_" + texIdx.ToString() + ".DDS";
				if (!File.Exists(texPath))
				{
					File.Copy(ddsPath, texPath);
					AssetDatabase.ImportAsset(texPath);
				}
			}
		}

		public void ImportModels()
		{
			_meshLookup = new Mesh[_f.ModelFiles.Count];
			for (int i = 0; i < _f.ModelFiles.Count; ++i)
			{
				var zmsPath = rootPath + _f.ModelFiles[i];
				if (!File.Exists(zmsPath))
				{
					Debug.LogWarning("Failed to find referenced ZMS.");
					continue;
				}

				var mesh = new Mesh();

				var zms = new Revise.Files.ZMS.ModelFile();
				zms.Load(zmsPath);

				var verts = new Vector3[zms.Vertices.Count];
				var uvs = new Vector2[zms.Vertices.Count];
				for (int k = 0; k < zms.Vertices.Count; ++k)
				{
					var v = zms.Vertices[k];
					v.TextureCoordinates[0].y = 1 - v.TextureCoordinates[0].y;

					verts[k] =  rtuPosition(v.Position);
					uvs[k] = v.TextureCoordinates[0];
				}
				mesh.vertices = verts;
				mesh.uv = uvs;

				int[] indices = new int[zms.Indices.Count * 3];
				for (int k = 0; k < zms.Indices.Count; ++k)
				{
					indices[k * 3 + 0] = zms.Indices[k].X;
					indices[k * 3 + 2] = zms.Indices[k].Y;
					indices[k * 3 + 1] = zms.Indices[k].Z;
				}
				mesh.triangles = indices;

				mesh.RecalculateNormals();
				Unwrapping.GenerateSecondaryUVSet(mesh);

				var meshPath = _basePath + "Mesh_" + i.ToString() + ".asset";
				AssetDatabase.CreateAsset(mesh, meshPath);
				_meshLookup[i] = mesh;
			}

			_matLookup = new Material[_f.TextureFiles.Count];
			for (int i = 0; i < _f.TextureFiles.Count; ++i)
			{
				var tex = _f.TextureFiles[i];

				var texPath = _basePath + "Tex_" + texLookup[i].ToString() + ".DDS";
				if (!File.Exists(texPath))
				{
					continue;
				}

				var tex2d = AssetDatabase.LoadAssetAtPath(texPath, typeof(Texture2D)) as Texture2D;

				Shader shader = null;
				if (tex.TwoSided) {
					if (tex.AlphaTestEnabled)
						shader = Shader.Find("Transparent/Cutout/DoubleSided");
					else if (tex.AlphaEnabled)
						shader = Shader.Find("Transparent/DoubleSided");
					else {
						Debug.LogWarning("Two-sided non-alpha material encountered.");
					}
				} else if (tex.AlphaTestEnabled)
					shader = Shader.Find("Transparent/Cutout/Diffuse");
				else if (tex.AlphaEnabled)
					shader = Shader.Find("Transparent/Diffuse");
				else
					shader = Shader.Find("Diffuse");

				if (!shader)
				{
					Debug.LogWarning("Failed to find appropriate shader for material.");
					continue;
				}

				var mat = new Material(shader);
				if (tex.AlphaTestEnabled)
					mat.SetFloat("_Cutoff", (float)tex.AlphaReference / 256);
				mat.mainTexture = tex2d;

				var matPath = _basePath + "Mat_" + i.ToString() + ".mat";
				AssetDatabase.CreateAsset(mat, matPath);

				_matLookup[i] = mat;
			}

			for (int i = 0; i < _f.Objects.Count; ++i)
			{
				var obj = _f.Objects[i];
				var mdl = ScriptableObject.CreateInstance<RoseMapObjectData>();

				for (int j = 0; j < obj.Parts.Count; ++j)
				{
					var part = obj.Parts[j];
					var subObj = new RoseMapObjectData.SubObject();

					subObj.mesh = _meshLookup[part.Model];
					subObj.material = _matLookup[part.Texture];
					subObj.animation = null;
					subObj.parent = part.Parent;
					subObj.position = rtuPosition(part.Position) / 100;
					subObj.rotation = rtuRotation(part.Rotation);
					subObj.scale = rtuScale(part.Scale);
					if (part.Collision == Revise.Files.ZSC.CollisionType.None) {
						subObj.colMode = 0;
					} else {
						subObj.colMode = 1;
					}

					if (part.AnimationFilePath != "") {
						var animPath = _basePath + "Anim_" + i.ToString() + "_" + j.ToString() + ".asset";
						var clip = ImportNodeAnimation(animPath, part.AnimationFilePath);
						subObj.animation = clip;
					}

					mdl.subObjects.Add(subObj);
				}

				var mdlPath = _basePath + "Model_" + i.ToString() + ".asset";
				AssetDatabase.CreateAsset(mdl, mdlPath);
				EditorUtility.SetDirty(mdl);
			}
		}
	}

	static AnimationClip ImportNodeAnimation(string clipPath, string zmoPath) {
		var f = new Revise.Files.ZMO.MotionFile();
		f.Load(rootPath + zmoPath);

		var clip = new AnimationClip();
		clip.wrapMode = WrapMode.Loop;
		clip.frameRate = f.FramesPerSecond;

		for (int i = 0; i < f.ChannelCount; ++i) {
			if (f[i].Index != 0) {
				Debug.LogWarning("Invalid channel index encountered");
				continue;
			}

			if (f[i].Type == Revise.Files.ZMO.ChannelType.Rotation) {
				var c = f[i] as Revise.Files.ZMO.Channels.RotationChannel;
				var curvex = new AnimationCurve();
				var curvey = new AnimationCurve();
				var curvez = new AnimationCurve();
				var curvew = new AnimationCurve();
				for (int j = 0; j < f.FrameCount; ++j) {
					var frame = rtuRotation(c.Frames[j]);
					curvex.AddKey((float)j / (float)f.FramesPerSecond, frame.x);
					curvey.AddKey((float)j / (float)f.FramesPerSecond, frame.y);
					curvez.AddKey((float)j / (float)f.FramesPerSecond, frame.z);
					curvew.AddKey((float)j / (float)f.FramesPerSecond, frame.w);
				}
				clip.SetCurve("", typeof(Transform), "localRotation.x", curvex);
				clip.SetCurve("", typeof(Transform), "localRotation.y", curvey);
				clip.SetCurve("", typeof(Transform), "localRotation.z", curvez);
				clip.SetCurve("", typeof(Transform), "localRotation.w", curvew);
			} else if (f[i].Type == Revise.Files.ZMO.ChannelType.Position) {
				var c = f[i] as Revise.Files.ZMO.Channels.PositionChannel;
				var curvex = new AnimationCurve();
				var curvey = new AnimationCurve();
				var curvez = new AnimationCurve();
				for (int j = 0; j < f.FrameCount; ++j) {
					var frame = rtuPosition(c.Frames[j]) / 100;
					curvex.AddKey((float)j / (float)f.FramesPerSecond, frame.x);
					curvey.AddKey((float)j / (float)f.FramesPerSecond, frame.y);
					curvez.AddKey((float)j / (float)f.FramesPerSecond, frame.z);
				}
				clip.SetCurve("", typeof(Transform), "localPosition.x", curvex);
				clip.SetCurve("", typeof(Transform), "localPosition.y", curvey);
				clip.SetCurve("", typeof(Transform), "localPosition.z", curvez);
			} else {
				Debug.LogWarning("Encountered unknown channel type.");
			}
		}

		AssetDatabase.CreateAsset(clip, clipPath);
		return clip;
	}

	
	

	static Texture2D ImportPlanMap(string planet, string map, int x, int y) {
		string mapPath = planet + "/" + map + "/" + x.ToString() + "_" + y.ToString() + "PLANMAP.DDS";
		string assetPath = "Assets/ROSEPmaps/" + mapPath;
		if (!File.Exists(assetPath)) {
			File.Copy(rootPath + "3DDATA/MAPS/" + mapPath, assetPath);
			AssetDatabase.ImportAsset(assetPath);
		}
		return Resources.LoadAssetAtPath(assetPath, typeof(Texture2D)) as Texture2D;
	}

	static Vector3 ifotruPosition(Vector3 v) {
		return (rtuPosition(v) / 100) + new Vector3(80, 0, 80);
	}

	static void ImportObject(string set, int x, int y, string prefix, int i, Revise.Files.IFO.Blocks.MapBlock obj)
	{
		var blockName = x.ToString() + "_" + y.ToString();
		string mdlBasePath = "Assets/ROSEMdls/" + set + "/";
		var mdlName = prefix + "_" + obj.ObjectID.ToString() + " (" + blockName + "_" + i.ToString() + ")";

		Object.DestroyImmediate(GameObject.Find(mdlName));

		// Temporarily disable objects while working on Terrain!
		//return;

		var mdlBaseName = mdlBasePath + "Model_" + obj.ObjectID.ToString();
		var mdlPath = mdlBaseName + ".asset";
		RoseMapObjectData modata = Resources.LoadAssetAtPath(mdlPath, typeof(RoseMapObjectData)) as RoseMapObjectData;

		if (!modata)
		{
			Debug.Log("Failed to find map model - " + mdlPath);
			return;
		}

		var go = new GameObject();
		var mo = go.AddComponent<RoseMapObject>();
		mo.data = modata;
		mo.UpdateModels();

		go.transform.localPosition = ifotruPosition(obj.Position);
		go.transform.localRotation = rtuRotation(obj.Rotation);
		go.transform.localScale = rtuScale(obj.Scale);
		go.isStatic = true;
		go.name = mdlName;
	}

	static void ImportMap(string planet, string map, Revise.Files.ZON.ZoneFile zon) {
		for (var i = 0; i < zon.Textures.Count; ++i) {
			var tex = zon.Textures[i];
		}

		for (var i = 0; i < zon.Tiles.Count; ++i) {
			var tile = zon.Tiles[i];
			Debug.Log(
				i.ToString() + ": " +
				tile.TileType.ToString() + "," +
				tile.Layer1.ToString() + "," + 
				tile.Layer2.ToString() + "," + 
				tile.Offset1.ToString() + "," + 
				tile.Offset2.ToString() + "," +
				tile.Rotation.ToString());
			Debug.Log(zon.Textures[tile.Offset1 + tile.Layer1]);
			Debug.Log(zon.Textures[tile.Offset2 + tile.Layer2]);
		}
	}

	static Terrain ImportTerrain(string planet, string map, Revise.Files.ZON.ZoneFile zon, int x, int y)
	{
		var blockName = x.ToString() + "_" + y.ToString();
		var basePath = rootPath + "3DDATA/MAPS/" + planet + "/" + map + "/" + blockName;
		float blockX = (x - 32) * 160;
		float blockY = (32 - y) * 160;

		Object.DestroyImmediate(GameObject.Find(blockName));

		var ifo = new Revise.Files.IFO.MapDataFile();
		ifo.Load(basePath + ".IFO");
		
		for (int i = 0; i < ifo.Objects.Count; ++i)
		{
			var obj = ifo.Objects[i];
			ImportObject("JUNON_JDT_DECO", x, y, "DECO", i, obj);
		}

		for (int i = 0; i < ifo.Buildings.Count; ++i)
		{
			var obj = ifo.Buildings[i];
			ImportObject("JUNON_JDT_CNST", x, y, "CNST", i, obj);
		}

		for (int i = 0; i < ifo.Animations.Count; ++i)
		{
			//var obj = ifo.Animations[i];
			Debug.LogWarning("Got unexpected animation object.");
		}

		for (int i = 0; i < ifo.Sounds.Count; ++i) {
			var snd = ifo.Sounds[i];
			var sndName = "SND_" + snd.ObjectID.ToString() + " (" + blockName + "_" + i.ToString() + ")";

			var a = new GameObject();

			//var s = a.AddComponent<AudioSource>();
			//TODO: Need to link to audio after copy in prestage

			a.transform.localPosition = ifotruPosition(snd.Position);
			a.transform.localRotation = rtuRotation(snd.Rotation);
			a.transform.localScale = rtuScale(snd.Scale);
			a.name = sndName;
			a.isStatic = true;
		}

		var tex = ImportPlanMap(planet, map, x, y);

		var him = new Revise.Files.HIM.HeightmapFile();
		him.Load(basePath + ".HIM");

		float[,] heights = new float[65,65];
		float heightMin = him.Heights[0, 0];
		float heightMax = him.Heights[0, 0];
		for (int ix = 0; ix < 65; ++ix)
		{
			for (int iy = 0; iy < 65; ++iy)
			{
				if (him.Heights[ix, iy] < heightMin)
				{
					heightMin = him.Heights[ix, iy];
				}
				if (him.Heights[ix, iy] > heightMax)
				{
					heightMax = him.Heights[ix, iy];
				}
			}
		}
		float heightBase = heightMin;
		float heightDelta = heightMax - heightMin;
		for (int ix = 0; ix < 65; ++ix)
		{
			for (int iy = 0; iy < 65; ++iy)
			{
				heights[ix, iy] = (him.Heights[64 - ix, iy] - heightBase) / heightDelta;
			}
		}

		var til = new Revise.Files.TIL.TileFile();
		til.Load(basePath + ".TIL");

		/*
		for (int ix = 0; ix < til.Width; ++ix) {
			for (int iy = 0; iy < til.Height; ++iy) {
				var t = til[ix, iy].Tile;
				Debug.Log(
					til[ix, iy].Brush.ToString() + "," +
					til[ix, iy].TileSet.ToString() + "," +
					til[ix, iy].TileIndex.ToString() + "," +
					til[ix, iy].Tile.ToString());
				Debug.Log(
					zon.Tiles[t].Layer1.ToString() + "," +
					zon.Tiles[t].Offset1.ToString() + "," +
					zon.Tiles[t].Layer2.ToString() + "," +
					zon.Tiles[t].Offset2.ToString() + "," +
					zon.Tiles[t].TileType.ToString() + "," +
					zon.Tiles[t].TileType.ToString() + "," +
					zon.Tiles[t].Rotation.ToString());
				Debug.Log(zon.Textures[zon.Tiles[t].Layer1 + zon.Tiles[t].Offset1]);
				Debug.Log(zon.Textures[zon.Tiles[t].Layer2 + zon.Tiles[t].Offset2]);
			}
		}
		*/

		var td = new TerrainData();
		td.size = new Vector3(80, heightDelta/100, 80);
		td.heightmapResolution = 65;
		td.SetHeights(0, 0, heights);
		var ts = new SplatPrototype[1];
		ts[0] = new SplatPrototype();
		ts[0].texture = tex;
		ts[0].tileSize = new Vector2(160, 160);
		td.splatPrototypes = ts;

		var ter = Terrain.CreateTerrainGameObject(td).GetComponent<Terrain>();
		ter.name = blockName;

		ter.transform.localPosition = new Vector3(blockX, heightBase/100, blockY);
		return ter;
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
	static Vector3 rtuScale(Vector3 v) {
		return new Vector3(v.x, v.z, v.y);
	}
}
