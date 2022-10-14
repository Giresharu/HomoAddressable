using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Exception = System.Exception;
using Object = UnityEngine.Object;

namespace HomoAddressableTools {
	public static class HomoAddressable {

#if UNITY_EDITOR || DEVELOPMENT_BUILD
		static HomoAddressableConfig Config {
			get {
				if (_config == null) {
					// string[] guids = UnityEditor.AssetDatabase.FindAssets("t:" + nameof(HomoAddressableConfig));
					var assets = Resources.FindObjectsOfTypeAll<HomoAddressableConfig>();
					if (assets.Length == 0) {
						var asset = ScriptableObject.CreateInstance<HomoAddressableConfig>();
#if UNITY_EDITOR
						if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))
							UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
						UnityEditor.AssetDatabase.CreateAsset(asset, "Assets/Resources/HomoAddressableConfig.asset");
						UnityEditor.AssetDatabase.SaveAssets();
#endif
						_config = asset;
					} else {
						// string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
						// _config = UnityEditor.AssetDatabase.LoadAssetAtPath<HomoAddressableConfig>(path);
						_config = assets[0];
					}
				}
				return _config;
			}
		}

		static HomoAddressableConfig _config;
#endif

		/// <summary>
		/// 以 Addressable 路径同步读取资源，当绑定的 HomoBehaviour 实例或实现了 IOnDestroyAssetLoad 接口的 MonoBehaviour 类被销毁时，会自动释放引用计数。
		/// </summary>
		/// <param name="bindObj">绑定的 HomoBehaviour 实例</param>
		/// <param name="path">读取的 Addressable 路径，默认情况下就是项目中的相对路径</param>
		/// <param name="failedCallback">读取失败时的回调</param>
		/// <typeparam name="T">读取的资源的类型</typeparam>
		/// <returns></returns>
		public static T LoadAsset<T>(this IOnDestroyAssetLoad bindObj, string path, Action failedCallback = null) {

			AsyncOperationHandle<T> handle;

			if (bindObj.Handles == null) {
				bindObj.Handles = new Dictionary<string, AsyncOperationHandle>();
				bindObj.OnDestroyEvent += Release;
			}
			if (bindObj.Handles.Count > 0 && bindObj.Handles.ContainsKey(path)) {
				handle = bindObj.Handles[path].Convert<T>();
				if (handle.Status == AsyncOperationStatus.Succeeded) {
					Report("Asset", path);
					return handle.Result;
				}
			} else {
				handle = Addressables.LoadAssetAsync<T>(path);
				bindObj.Handles[path] = handle;
			}

			var result = handle.WaitForCompletion();

			if (handle.Status == AsyncOperationStatus.Failed) {
				CatchFailed(bindObj, handle, path, null, failedCallback);
				throw new OperationCanceledException();
			}
			Report("Asset", path);
			return result;
		}

		/// <summary>
		/// 以 AssetReference 同步读取资源，当绑定的 HomoBehaviour 实例或实现了 IOnDestroyAssetLoad 接口的 MonoBehaviour 类被销毁时，会自动释放引用计数。
		/// </summary>
		/// <param name="bindObj">绑定的 HomoBehaviour 实例</param>
		/// <param name="reference">读取的 AssetReference</param>
		/// <param name="failedCallback">读取失败时的回调</param>
		/// <typeparam name="T">读取的资源的类型</typeparam>
		/// <returns></returns>
		public static T LoadAsset<T>(this IOnDestroyAssetLoad bindObj, AssetReference reference, Action failedCallback = null) {

			AsyncOperationHandle<T> handle;

			if (bindObj.Handles == null) {
				bindObj.Handles = new Dictionary<string, AsyncOperationHandle>();
				bindObj.OnDestroyEvent += Release;
			}
			if (bindObj.Handles.Count > 0 && bindObj.Handles.ContainsKey(reference.AssetGUID)) {
				handle = bindObj.Handles[reference.AssetGUID].Convert<T>();
				if (handle.Status == AsyncOperationStatus.Succeeded) {

					Report("Asset", reference.AssetGUID);
					return handle.Result;
				}
			} else {
				handle = reference.LoadAssetAsync<T>();
				bindObj.Handles[reference.AssetGUID] = handle;
			}

			var result = handle.WaitForCompletion();

			if (handle.Status == AsyncOperationStatus.Failed) {
				CatchFailed(bindObj, handle, null, reference, failedCallback);
				throw new OperationCanceledException();
			}

			Report("Asset", reference.AssetGUID);
			return result;
		}

		/// <summary>
		/// 以 Addressable 路径异步读取资源，当绑定的 HomoBehaviour 实例或实现了 IOnDestroyAssetLoad 接口的 MonoBehaviour 类被销毁时，会自动释放引用计数。
		/// </summary>
		/// <param name="bindObj">绑定的 HomoBehaviour 实例</param>
		/// <param name="path">读取的 Addressable 路径，默认情况下就是项目中的相对路径</param>
		/// <param name="progress">可以填写 Progress 实例,将会以读取进度作为参数执行该实例中的委托 </param>
		/// <param name="timing">执行异步的时间点</param>
		/// <param name="failedCallback">读取失败时的回调</param>
		/// <param name="timeoutCallback">读取超时时的回调</param>
		/// <param name="canceledCallback">读取取消时的回调</param>
		/// <param name="millisecondsTimeout">超时时间</param>
		/// <param name="token">取消信号</param>
		/// <typeparam name="T">读取的资源的类型</typeparam>
		/// <returns></returns>
		/// <exception cref="OperationCanceledException"></exception>
		public static async UniTask<T> LoadAssetAsync<T>(this IOnDestroyAssetLoad bindObj, string path, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, Action failedCallback = null, Action timeoutCallback = null, Action canceledCallback = null, int millisecondsTimeout = 0, CancellationToken token = default) {

			var destroyToken = (bindObj as MonoBehaviour).GetCancellationTokenOnDestroy();
			var cancel = CancellationTokenSource.CreateLinkedTokenSource(token, destroyToken);

			if (cancel.Token.IsCancellationRequested) {
				Report("Asset", path, LogType.Canceled);
				canceledCallback?.Invoke();
				throw new OperationCanceledException();
			}

			AsyncOperationHandle<T> handle;

			if (bindObj.Handles == null) {
				bindObj.Handles = new Dictionary<string, AsyncOperationHandle>();
				bindObj.OnDestroyEvent += Release;
			}
			if (bindObj.Handles.Count > 0 && bindObj.Handles.ContainsKey(path)) {
				handle = bindObj.Handles[path].Convert<T>();
				if (handle.Status == AsyncOperationStatus.Succeeded) {
					Report("Asset", path);
					return handle.Result;
				}
			} else {
				handle = Addressables.LoadAssetAsync<T>(path);
				bindObj.Handles[path] = handle;
			}

			T result;

			try {
				result = await TryAwaitHandle(bindObj, handle, path, null, progress, timing, timeoutCallback, canceledCallback, millisecondsTimeout, cancel);
			} catch (Exception e) when (e is not OperationCanceledException) {
				CatchFailed(bindObj, handle, path, null, failedCallback, cancel);
				throw;
			}

			Report("Asset", path);
			return result;
		}

		/// <summary>
		/// 以 AssetReference 异步读取资源，当绑定的 HomoBehaviour 实例或实现了 IOnDestroyAssetLoad 接口的 MonoBehaviour 类被销毁时，会自动释放引用计数。
		/// </summary>
		/// <param name="bindObj">绑定的 HomoBehaviour 实例</param>
		/// <param name="reference">读取的 AssetReference</param>
		/// <param name="progress">可以填写 Progress 实例,将会以读取进度作为参数执行该实例中的委托 </param>
		/// <param name="timing">执行异步的时间点</param>
		/// <param name="failedCallback">读取失败时的回调</param>
		/// <param name="timeoutCallback">读取超时时的回调</param>
		/// <param name="canceledCallback">读取取消时的回调</param>
		/// <param name="millisecondsTimeout">超时时间</param>
		/// <param name="token">取消信号</param>
		/// <typeparam name="T">读取的资源的类型</typeparam>
		/// <returns></returns>
		public static async UniTask<T> LoadAssetAsync<T>(this IOnDestroyAssetLoad bindObj, AssetReference reference, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, Action failedCallback = null, Action timeoutCallback = null, Action canceledCallback = null, int millisecondsTimeout = 0, CancellationToken token = default) {

			var destroyToken = (bindObj as MonoBehaviour).GetCancellationTokenOnDestroy();
			var cancel = CancellationTokenSource.CreateLinkedTokenSource(token, destroyToken);

			if (cancel.Token.IsCancellationRequested) {

				Report("Asset", reference.AssetGUID, LogType.Canceled);

				canceledCallback?.Invoke();
				throw new OperationCanceledException();
			}

			AsyncOperationHandle<T> handle;

			if (bindObj.Handles == null) {
				bindObj.Handles = new Dictionary<string, AsyncOperationHandle>();
				bindObj.OnDestroyEvent += Release;
			}
			if (bindObj.Handles.Count > 0 && bindObj.Handles.ContainsKey(reference.AssetGUID)) {
				handle = bindObj.Handles[reference.AssetGUID].Convert<T>();
				if (handle.Status == AsyncOperationStatus.Succeeded) {
					Report("Asset", reference.AssetGUID);
					return handle.Result;
				}
			} else {
				handle = Addressables.LoadAssetAsync<T>(reference);
				bindObj.Handles[reference.AssetGUID] = handle;
			}

			T result;
			try {
				result = await TryAwaitHandle(bindObj, handle, null, reference, progress, timing, timeoutCallback, canceledCallback, millisecondsTimeout, cancel);
			} catch (Exception e) when (e is not OperationCanceledException) {
				CatchFailed(bindObj, handle, null, reference, failedCallback, cancel);
				throw;
			}

			Report("Asset", reference.AssetGUID);
			return result;
		}

		/// <summary>
		/// 原版 Addressable.LoadAssetAsync ，提供给强迫症使用
		/// </summary>
		/// <param name="path">读取的 Addressable 路径，默认情况下就是项目中的相对路径</param>
		/// <typeparam name="T">读取的资源的类型</typeparam>
		/// <returns></returns>
		public static AsyncOperationHandle<T> LoadAssetHandle<T>(string path) {
			return Addressables.LoadAssetAsync<T>(path);
		}

		/// <summary>
		/// 原版 Addressable.LoadAssetAsync ，提供给强迫症使用
		/// </summary>
		/// <param name="reference">读取的 AssetReference</param>
		/// <typeparam name="T">读取的资源的类型</typeparam>
		/// <returns></returns>
		public static AsyncOperationHandle<T> LoadAssetHandle<T>(AssetReference reference) {
			return reference.LoadAssetAsync<T>();
		}

		/// <summary>
		/// 以 Addressable 路径同步读取预制体的依赖资源并实例化之。
		/// </summary>
		/// <param name="path">读取的 Addressable 路径，默认情况下就是项目中的相对路径</param>
		/// <param name="parent">实例化在场景中的父节点</param>
		/// <param name="failedCallback">读取失败时的回调</param>
		/// <returns></returns>
		public static GameObject Instantiate(string path, Transform parent = null, Action failedCallback = null) {

			var handle = Addressables.InstantiateAsync(path, parent);

			var result = handle.WaitForCompletion();

			if (handle.Status == AsyncOperationStatus.Failed) {
				CatchFailed(null, handle, path, null, failedCallback, objType: "Prefab");
				throw new OperationCanceledException();
			}
			Report("Prefab", path);
			return result;
		}

		/// <summary>
		/// 以 AssetReferenceGameObject 同步读取预制体的依赖资源并实例化之。
		/// </summary>
		/// <param name="reference">读取的 AssetReferenceGameObject</param>
		/// <param name="parent">实例化在场景中的父节点</param>
		/// <param name="failedCallback">读取失败时的回调</param>
		/// <returns></returns>
		public static GameObject Instantiate(AssetReference reference, Transform parent = null, Action failedCallback = null) {

			var handle = Addressables.InstantiateAsync(reference, parent);

			var result = handle.WaitForCompletion();

			if (handle.Status == AsyncOperationStatus.Failed) {
				CatchFailed(null, handle, null, reference, failedCallback, objType: "Prefab");
				throw new OperationCanceledException();
			}
			Report("Prefab", reference.AssetGUID);
			return result;
		}

		/// <summary>
		/// 以 Addressable 路径异步读取预制体的依赖资源并实例化之。
		/// </summary>
		/// <param name="path">读取的 Addressable 路径，默认情况下就是项目中的相对路径</param>
		/// <param name="parent">实例化在场景中的父节点</param>
		/// <param name="progress">可以填写 Progress 实例,将会以读取进度作为参数执行该实例中的委托 </param>
		/// <param name="failedCallback">读取失败时的回调</param>
		/// <param name="timeoutCallback">读取超时时的回调</param>
		/// <param name="canceledCallback">读取取消时的回调</param>
		/// <param name="millisecondsTimeout">超时时间</param>
		/// <param name="timing">执行异步的时间点</param>
		/// <param name="token">取消信号</param>
		/// <returns></returns>
		public static async UniTask<GameObject> InstantiateAsync(string path, Transform parent = null, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, Action failedCallback = null, Action timeoutCallback = null, Action canceledCallback = null, int millisecondsTimeout = 0, CancellationToken token = default) {

			if (token.IsCancellationRequested) {
				Report("Prefab", path, LogType.Canceled);
				canceledCallback?.Invoke();
				throw new OperationCanceledException();
			}

			GameObject result;
			var handle = Addressables.InstantiateAsync(path, parent);

			try {
				result = await TryAwaitHandle(null, handle, path, null, progress, timing, timeoutCallback, canceledCallback, millisecondsTimeout, null, token, "Prefab");
			} catch (Exception e) when (e is not OperationCanceledException) {
				CatchFailed(null, handle, path, null, failedCallback, null, "Prefab");
				throw;
			}
			Report("Prefab", path);
			return result;
		}

		/// <summary>
		/// 以 AssetReferenceGameObject 异步读取预制体的依赖资源并实例化之。
		/// </summary>
		/// <param name="reference">读取的 AssetReferenceGameObject</param>
		/// <param name="parent">实例化在场景中的父节点</param>
		/// <param name="progress">可以填写 Progress 实例,将会以读取进度作为参数执行该实例中的委托 </param>
		/// <param name="failedCallback">读取失败时的回调</param>
		/// <param name="timeoutCallback">读取超时时的回调</param>
		/// <param name="canceledCallback">读取取消时的回调</param>
		/// <param name="millisecondsTimeout">超时时间</param>
		/// <param name="timing">执行异步的时间点</param>
		/// <param name="token">取消信号</param>
		/// <returns></returns>
		public static async UniTask<GameObject> InstantiateAsync(AssetReference reference, Transform parent = null, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, Action failedCallback = null, Action timeoutCallback = null, Action canceledCallback = null, int millisecondsTimeout = 0, CancellationToken token = default) {

			if (token.IsCancellationRequested) {
				Report("Prefab", reference.AssetGUID, LogType.Canceled);
				canceledCallback?.Invoke();
				throw new OperationCanceledException();
			}

			GameObject result;
			var handle = reference.InstantiateAsync(parent);

			try {
				result = await TryAwaitHandle(null, handle, null, reference, progress, timing, timeoutCallback, canceledCallback, millisecondsTimeout, null, token, "Prefab");
			} catch (Exception e) when (e is not OperationCanceledException) {
				CatchFailed(null, handle, null, reference, failedCallback, null, "Prefab");
				throw;
			}
			Report("Prefab", reference.AssetGUID);
			return result;
		}

		/// <summary>
		/// 以 Addressable 路径同步读取场景实例及其依赖资源，也可以自动加载。。
		/// </summary>
		/// <param name="path">读取的 Addressable 路径，默认情况下就是项目中的相对路径</param>
		/// <param name="mode">加载场景的方式，Single 会卸载当前场景，Additive 则会保留</param>
		/// <param name="activeOnLoad">为 true 时，读取后会自动加载</param>
		/// <param name="failedCallback">读取失败时的回调</param>
		/// <returns></returns>
		public static SceneInstance LoadScene(string path, LoadSceneMode mode = LoadSceneMode.Single, bool activeOnLoad = true, Action failedCallback = null) {
			var handle = Addressables.LoadSceneAsync(path, mode, activeOnLoad);
			var result = handle.WaitForCompletion();
			if (handle.Status == AsyncOperationStatus.Failed) {
				CatchFailed(null, handle, path, null, failedCallback);
				throw new OperationCanceledException();
			}

			Report("Asset", path);
			return result;
		}

		/// <summary>
		/// 以 AssetReference 同步读取场景实例及其依赖资源，也可以自动加载。。
		/// </summary>
		/// <param name="reference">读取的 AssetReference</param>
		/// <param name="mode">加载场景的方式，Single 会卸载当前场景，Additive 则会保留</param>
		/// <param name="activeOnLoad">为 true 时，读取后会自动加载</param>
		/// <param name="failedCallback">读取失败时的回调</param>
		/// <returns></returns>
		public static SceneInstance LoadScene(AssetReference reference, LoadSceneMode mode = LoadSceneMode.Single, bool activeOnLoad = true, Action failedCallback = null) {
			var handle = reference.LoadSceneAsync(mode, activeOnLoad);
			var result = handle.WaitForCompletion();
			if (handle.Status == AsyncOperationStatus.Failed) {
				CatchFailed(null, handle, null, reference, failedCallback);
				throw new OperationCanceledException();
			}
			Report("Asset", reference.AssetGUID);
			return result;
		}

		/// <summary>
		/// 以 Addressable 路径异步读取场景实例及其依赖资源，也可以自动加载。
		/// </summary>
		/// <param name="path">读取的 Addressable 路径，默认情况下就是项目中的相对路径</param>
		/// <param name="mode">加载场景的方式，Single 会卸载当前场景，Additive 则会保留</param>
		/// <param name="activeOnLoad">为 true 时，读取后会自动加载</param>
		/// <param name="progress">可以填写 Progress 实例,将会以读取进度作为参数执行该实例中的委托 </param>
		/// <param name="timing">执行异步的时间点</param>
		/// <param name="failedCallback">读取失败时的回调</param>
		/// <param name="timeoutCallback">读取超时时的回调</param>
		/// <param name="canceledCallback">读取取消时的回调</param>
		/// <param name="millisecondsTimeout">超时时间</param>
		/// <param name="token">取消信号</param>
		/// <returns></returns>
		public static async UniTask<SceneInstance> LoadSceneAsync(string path, LoadSceneMode mode = LoadSceneMode.Single, bool activeOnLoad = true, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, Action failedCallback = null, Action timeoutCallback = null, Action canceledCallback = null, int millisecondsTimeout = 0, CancellationToken token = default) {

			if (token.IsCancellationRequested) {
				Report("SceneInstance", path, LogType.Canceled);
				canceledCallback?.Invoke();
				throw new OperationCanceledException();
			}
			SceneInstance result;
			var handle = Addressables.LoadSceneAsync(path, mode, activeOnLoad);
			try {
				result = await TryAwaitHandle(null, handle, path, null, progress, timing, timeoutCallback, canceledCallback, millisecondsTimeout, null, token, "SceneInstance");
			} catch (Exception e) when (e is not OperationCanceledException) {
				CatchFailed(null, handle, path, null, failedCallback, null, "SceneInstance");
				throw;
			}
			Report("SceneInstance", path);
			return result;
		}

		/// <summary>
		/// 以 AssetReference 异步读取场景实例及其依赖资源，也可以自动加载。
		/// </summary>
		/// <param name="reference">读取的 AssetReference</param>
		/// <param name="mode">加载场景的方式，Single 会卸载当前场景，Additive 则会保留</param>
		/// <param name="activeOnLoad">为 true 时，读取后会自动加载</param>
		/// <param name="progress">可以填写 Progress 实例,将会以读取进度作为参数执行该实例中的委托 </param>
		/// <param name="timing">执行异步的时间点</param>
		/// <param name="failedCallback">读取失败时的回调</param>
		/// <param name="timeoutCallback">读取超时时的回调</param>
		/// <param name="canceledCallback">读取取消时的回调</param>
		/// <param name="millisecondsTimeout">超时时间</param>
		/// <param name="token">取消信号</param>
		public static async UniTask<SceneInstance> LoadSceneAsync(AssetReference reference, LoadSceneMode mode = LoadSceneMode.Single, bool activeOnLoad = true, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, Action failedCallback = null, Action timeoutCallback = null, Action canceledCallback = null, int millisecondsTimeout = 0, CancellationToken token = default) {

			if (token.IsCancellationRequested) {
				Report("SceneInstance", reference.AssetGUID, LogType.Canceled);
				canceledCallback?.Invoke();
				throw new OperationCanceledException();
			}
			SceneInstance result;
			var handle = reference.LoadSceneAsync(mode, activeOnLoad);
			try {
				result = await TryAwaitHandle(null, handle, null, reference, progress, timing, timeoutCallback, canceledCallback, millisecondsTimeout, null, token, "SceneInstance");
			} catch (Exception e) when (e is not OperationCanceledException) {
				CatchFailed(null, handle, null, reference, failedCallback, null, "SceneInstance");
				throw;
			}
			Report("SceneInstance", reference.AssetGUID);
			return result;
		}

		static async UniTask<T> TryAwaitHandle<T>(IOnDestroyAssetLoad bindObj = null, AsyncOperationHandle<T> handle = default, string path = null, AssetReference reference = null, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, Action timeoutCallback = null, Action canceledCallback = null, int millisecondsTimeout = 0, CancellationTokenSource cancel = null, CancellationToken token = default, string objType = "Asset") {

			T result;
			bool isCanceled;

			var uniTask = handle.ToUniTask(progress, timing, cancel?.Token ?? token);

			if (millisecondsTimeout > 0) {
				var timeout = UniTask.Delay(millisecondsTimeout, DelayType.Realtime, timing, token).AsAsyncUnitUniTask();
				int win;
				(isCanceled, (win, result, _)) = await UniTask.WhenAny(uniTask, timeout).SuppressCancellationThrow();

				if (win == 1) {
					Report(objType, path ?? reference?.AssetGUID, LogType.Timeout);
					Addressables.Release(handle);
					if (cancel != null) {
						cancel.Cancel();
						cancel.Dispose();
					}
					bindObj?.Handles.Remove(path ?? reference?.AssetGUID ?? "");
					timeoutCallback?.Invoke();
					throw new OperationCanceledException();
				}
			} else (isCanceled, result) = await uniTask.SuppressCancellationThrow();

			if (isCanceled) {
				Report(objType, path ?? reference?.AssetGUID, LogType.Canceled);
				cancel?.Dispose();
				Addressables.Release(handle);
				bindObj?.Handles.Remove(path ?? reference?.AssetGUID ?? "");
				canceledCallback?.Invoke();
				throw new OperationCanceledException();
			}
			return result;
		}

		static void CatchFailed(IOnDestroyAssetLoad bindObj = null, AsyncOperationHandle handle = default, string path = null, AssetReference reference = null, Action failedCallback = null, CancellationTokenSource cancel = null, string objType = "Asset") {

			Report(objType, path ?? reference?.AssetGUID, LogType.Failed);
			if (cancel != null) {
				cancel.Cancel();
				cancel.Dispose();
			}
			Addressables.Release(handle);
			bindObj?.Handles.Remove(path ?? reference?.AssetGUID ?? "");
			failedCallback?.Invoke();

		}

		static void Release(object sender) {
			IOnDestroyAssetLoad obj = sender as IOnDestroyAssetLoad;
			if (obj == null) {
				Report(logType: LogType.Null);
				return;
			}
			foreach (var handle in obj.Handles.Values) {
				Addressables.Release(handle);
			}
			obj.Handles.Clear();
		}

		/// <summary>
		/// 释放从 Addressable 读取的预制体以及其依赖资源的引用计数，并销毁该预制体。
		/// </summary>
		/// <param name="obj">要销毁的预制体</param>
		public static void ReleaseInstance(this GameObject obj) {
			Addressables.ReleaseInstance(obj);
			Object.Destroy(obj);
		}

		/// <summary>
		/// 释放从 Addressable 读取的场景资源及其依赖资源的引用计数，并卸载之。
		/// </summary>
		/// <param name="sceneInstance">要卸载的场景实例</param>
		/// <param name="progress">卸载的进度</param>
		/// <param name="timing">执行异步的时间点</param>
		public static async UniTaskVoid ReleaseScene(this SceneInstance sceneInstance, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update) {
			await Addressables.UnloadSceneAsync(sceneInstance).ToUniTask(progress, timing);
		}

		static void Report(string objType = null, string path = null, LogType logType = default) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			var config = Config? Config: ScriptableObject.CreateInstance<HomoAddressableConfig>();
			string loadStatus = logType.ToString("G");
			string output;
			if (logType != LogType.Null) output = $"{objType} in [{path}] Loaded {loadStatus} !";
			else output = "The IOnDestroyAssetLoad object is null！Please check sender's type!";

			var logLevel = logType switch {
				LogType.Succeeded => config.SucceededLogLevel,
				LogType.Canceled => config.CanceledLogLevel,
				LogType.Timeout => config.TimeoutLogLevel,
				LogType.Failed => config.FailedLogLevel,
				LogType.Null => config.ReleaseSenderNullWarning,
				_ => HomoAddressableConfig.LogLevel.Log
			};

			switch (logLevel) {
				case HomoAddressableConfig.LogLevel.None:
					return;
				case HomoAddressableConfig.LogLevel.Log:
					Debug.Log(output);
					break;
				case HomoAddressableConfig.LogLevel.Warning:
					Debug.LogWarning(output);
					break;
				case HomoAddressableConfig.LogLevel.Error:
					Debug.LogError(output);
					break;
			}
#endif
		}

		enum LogType {
			Succeeded, Canceled, Timeout, Failed, Null
		}

	}


}