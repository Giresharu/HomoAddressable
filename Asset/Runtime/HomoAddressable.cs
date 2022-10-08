using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;


namespace HomoAddressableTools {

	public static class HomoAddressable {

		public static T LoadAsset<T>(this IOnDestroyAssetLoad bindObj, string path) where T: Object {

			if (bindObj.Handles == null) {
				bindObj.Handles = new Dictionary<string, AsyncOperationHandle<Object>>();
				bindObj.OnDestroyEvent += Release;
			}
			AsyncOperationHandle<Object> handle;
			if (bindObj.Handles.ContainsKey(path)) {
				handle = bindObj.Handles[path];
				if (handle.Status == AsyncOperationStatus.None)
					handle.WaitForCompletion();
				if (handle.Status == AsyncOperationStatus.Succeeded)
					return bindObj.Handles[path].Result as T;
			}
			handle = Addressables.LoadAssetAsync<Object>(path);
			bindObj.Handles[path] = handle;
			return handle.WaitForCompletion() as T;
		}

		public static T LoadAsset<T>(this IOnDestroyAssetLoad bindObj, AssetReference reference) where T: Object {

			if (bindObj.Handles == null) {
				bindObj.Handles = new Dictionary<string, AsyncOperationHandle<Object>>();
				bindObj.OnDestroyEvent += Release;
			}
			AsyncOperationHandle<Object> handle;
			if (bindObj.Handles.ContainsKey(reference.ToString())) {
				handle = bindObj.Handles[reference.ToString()];
				if (handle.Status == AsyncOperationStatus.None)
					handle.WaitForCompletion();
				if (handle.Status == AsyncOperationStatus.Succeeded)
					return bindObj.Handles[reference.ToString()].Result as T;
			}
			handle = reference.LoadAssetAsync<Object>();
			bindObj.Handles[reference.ToString()] = handle;
			return handle.WaitForCompletion() as T;
		}

		public static async UniTask<T> LoadAssetAsync<T>(this IOnDestroyAssetLoad bindObj, string path, IProgress<float> progress = null,
			PlayerLoopTiming timing = PlayerLoopTiming.Update,
			CancellationToken token = default) where T: Object {

			if (token == default) {
				token = (bindObj as MonoBehaviour).GetCancellationTokenOnDestroy();
			}

			if (token.IsCancellationRequested) throw new OperationCanceledException();

			if (bindObj.Handles == null) {
				bindObj.Handles = new Dictionary<string, AsyncOperationHandle<Object>>();
				bindObj.OnDestroyEvent += Release;
			}

			AsyncOperationHandle<Object> handle;
			if (bindObj.Handles.ContainsKey(path)) {
				handle = bindObj.Handles[path];
				if (handle.IsDone) return handle.Result as T;
				return await handle.ToUniTask(progress, timing, token) as T;
			}

			handle = Addressables.LoadAssetAsync<Object>(path);
			bindObj.Handles[path] = handle;
			return await handle.ToUniTask(progress, timing, token) as T;
		}

		public static AsyncOperationHandle<T> LoadAssetHandle<T>(string path) {
			return Addressables.LoadAssetAsync<T>(path);
		}

		public static AsyncOperationHandle<T> LoadAssetHandle<T>(AssetReference reference) {
			return reference.LoadAssetAsync<T>();
		}

		public static async UniTask<T> LoadAssetAsync<T>(this IOnDestroyAssetLoad bindObj, AssetReference reference, IProgress<float> progress = null,
			PlayerLoopTiming timing = PlayerLoopTiming.Update,
			CancellationToken token = default) where T: Object {

			if (token == default) {
				token = (bindObj as MonoBehaviour).GetCancellationTokenOnDestroy();
			}

			if (token.IsCancellationRequested) throw new OperationCanceledException();

			if (bindObj.Handles == null) {
				bindObj.Handles = new Dictionary<string, AsyncOperationHandle<Object>>();
				bindObj.OnDestroyEvent += Release;
			}

			AsyncOperationHandle<Object> handle;
			if (bindObj.Handles.ContainsKey(reference.ToString())) {
				handle = bindObj.Handles[reference.ToString()];
				if (handle.IsDone) return handle.Result as T;
				return await handle.ToUniTask(progress, timing, token) as T;
			}

			handle = reference.LoadAssetAsync<Object>();
			bindObj.Handles[reference.ToString()] = handle;
			return await handle.ToUniTask(progress, timing, token) as T;
		}

		public static GameObject Instantiate(string path, Transform parent = null) {
			var handle = Addressables.InstantiateAsync(path);
			var gameObject = handle.WaitForCompletion();
			if (parent != null)
				gameObject.transform.SetParent(parent);
			return gameObject;

		}
		public static GameObject Instantiate(AssetReference reference, Transform parent = null) {
			var handle = reference.InstantiateAsync();
			var gameObject = handle.WaitForCompletion();
			if (parent != null)
				gameObject.transform.SetParent(parent);
			return gameObject;
		}

		public static async UniTask<GameObject> InstantiateAsync(string path, Transform parent = null, IProgress<float> progress = null,
			PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken token = default) {

			var handle = Addressables.InstantiateAsync(path);
			var gameObject = await handle.ToUniTask(progress, timing, token);
			if (parent != null)
				gameObject.transform.SetParent(parent);
			return gameObject;
		}
		public static async UniTask<GameObject> InstantiateAsync(AssetReferenceGameObject reference, Transform parent = null, IProgress<float> progress = null,
			PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken token = default) {

			var handle = reference.InstantiateAsync();
			var result = await handle.ToUniTask(progress, timing, token);
			if (parent != null)
				result.transform.SetParent(parent);
			return result;
		}

		public static SceneInstance LoadScene(string path, LoadSceneMode mode = LoadSceneMode.Single, bool activeOnLoad = true) {
			var handle = Addressables.LoadSceneAsync(path, mode, activeOnLoad);
			return handle.WaitForCompletion();
		}
		public static SceneInstance LoadScene(AssetReference reference, LoadSceneMode mode = LoadSceneMode.Single, bool activeOnLoad = true) {
			var handle = reference.LoadSceneAsync(mode, activeOnLoad);
			return handle.WaitForCompletion();
		}

		public static async UniTask<SceneInstance> LoadSceneAsync(string path, LoadSceneMode mode = LoadSceneMode.Single, bool activeOnLoad = true, IProgress<float> progress = null,
			PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken token = default) {
			var handle = Addressables.LoadSceneAsync(path, mode, activeOnLoad);
			return await handle.ToUniTask(progress, timing, token);
		}

		public static async UniTask<SceneInstance> LoadSceneAsync(AssetReference reference, LoadSceneMode mode = LoadSceneMode.Single, bool activeOnLoad = true, IProgress<float> progress = null,
			PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken token = default) {
			var handle = reference.LoadSceneAsync(mode, activeOnLoad);
			return await handle.ToUniTask(progress, timing, token);
		}

		public static async UniTask<Sprite> LoadSpriteAsync(this IOnDestroyAssetLoad bindObj, string path, int index = -1, IProgress<float> progress = null,
			PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken token = default) {

			if (token.IsCancellationRequested) throw new OperationCanceledException();

			Regex regex = new Regex(@"[^/]*(?=\.[^/]*$)", RegexOptions.ExplicitCapture);
			string spriteName = regex.Match(path).Value;
			StringBuilder s = new StringBuilder();
			s.Append(path).Append("[").Append(spriteName);
			if (index != -1) s.Append($"_{index}");
			s.Append("]");
			return await bindObj.LoadAssetAsync<Sprite>(s.ToString(), progress, timing, token);
		}

		public static Sprite LoadSprite(this IOnDestroyAssetLoad bindObj, string path, int index = -1) {
			Regex regex = new Regex(@"[^/]*(?=\.[^/]*$)", RegexOptions.ExplicitCapture);
			string spriteName = regex.Match(path).Value;
			StringBuilder s = new StringBuilder();
			s.Append(path).Append("[").Append(spriteName);
			if (index != -1) s.Append($"_{index}");
			s.Append("]");
			return bindObj.LoadAsset<Sprite>(s.ToString());
		}

		public static async UniTask<Sprite> LoadSpriteAsync(this IOnDestroyAssetLoad bindObj, string path, string spriteName, IProgress<float> progress = null,
			PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken token = default) {

			if (token.IsCancellationRequested) throw new OperationCanceledException();

			StringBuilder s = new StringBuilder();
			s.Append(path).Append("[").Append(spriteName).Append("]");
			return await bindObj.LoadAssetAsync<Sprite>(s.ToString(), progress, timing, token);
		}

		public static Sprite LoadSprite(this IOnDestroyAssetLoad bindObj, string path, string spriteName) {
			StringBuilder s = new StringBuilder();
			s.Append(path).Append("[").Append(spriteName).Append("]");
			return bindObj.LoadAsset<Sprite>(s.ToString());
		}

		public static async UniTask<Sprite[]> LoadSpritesAsync(this IOnDestroyAssetLoad bindObj, string path, int count = 1, IProgress<float> progress = null,
			PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken token = default) {

			if (token.IsCancellationRequested) throw new OperationCanceledException();

			List<Sprite> sprites = new List<Sprite>();
			int index = 0;
			Regex regex = new Regex(@"[^/]*(?=\.[^/]*$)", RegexOptions.ExplicitCapture);
			string spriteName = regex.Match(path).Value;
			float[] percents = new float[count];
			while (index < count) {
				float percent = percents.Sum() / count;
				progress?.Report(percent);
				StringBuilder s = new StringBuilder();
				s.Append(path).Append("[").Append(spriteName).Append($"_{index}").Append("]");
				sprites.Add(await bindObj.LoadAssetAsync<Sprite>(s.ToString(), null, timing, token));
				percents[index] = 1;
				index++;
			}
			progress?.Report(percents.Sum() / count);
			return sprites.ToArray();
		}
		public static Sprite[] LoadSprites(this IOnDestroyAssetLoad bindObj, string path, int count = 1) {
			List<Sprite> sprites = new List<Sprite>();
			int index = 0;
			Regex regex = new Regex(@"[^/]*(?=\.[^/]*$)", RegexOptions.ExplicitCapture);
			string spriteName = regex.Match(path).Value;
			while (index < count) {
				StringBuilder s = new StringBuilder();
				s.Append(path).Append("[").Append(spriteName).Append($"_{index}").Append("]");
				sprites.Add(bindObj.LoadAsset<Sprite>(s.ToString()));
				index++;
			}
			return sprites.ToArray();
		}

		static void Release(object sender) {
			IOnDestroyAssetLoad obj = sender as IOnDestroyAssetLoad;
			if (obj == null) {
				Debug.LogWarning("The IOnDestroyAssetLoad object is null！Please check sender's type!");
				return;
			}
			foreach (var handle in obj.Handles.Values) {
				Addressables.Release(handle);
			}
			obj.Handles = null;
		}

		public static void ReleaseInstance(this GameObject obj) {
			Addressables.ReleaseInstance(obj);
			Object.Destroy(obj);
		}

		public static async UniTaskVoid ReleaseScene(this SceneInstance sceneInstance) {
			await Addressables.UnloadSceneAsync(sceneInstance);
		}

	}

}