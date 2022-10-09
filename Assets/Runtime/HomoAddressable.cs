using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;


namespace HomoAddressableTools {

	public static class HomoAddressable {
		/// <summary>
		/// 以 Addressable 路径同步读取资源，当绑定的 HomoBehaviour 实例或实现了 IOnDestroyAssetLoad 接口的 MonoBehaviour 类被销毁时，会自动释放引用计数。
		/// </summary>
		/// <param name="bindObj">绑定的 HomoBehaviour 实例</param>
		/// <param name="path">读取的 Addressable 路径，默认情况下就是项目中的相对路径</param>
		/// <typeparam name="T">读取的资源的类型</typeparam>
		/// <returns></returns>
		public static T LoadAsset<T>(this IOnDestroyAssetLoad bindObj, string path) {

			if (bindObj.Handles == null) {
				bindObj.Handles = new Dictionary<string, AsyncOperationHandle>();
				bindObj.OnDestroyEvent += Release;
			}
			AsyncOperationHandle<T> handle;
			if (bindObj.Handles.ContainsKey(path)) {
				handle = bindObj.Handles[path].Convert<T>();
				if (handle.Status == AsyncOperationStatus.None)
					handle.WaitForCompletion();
				if (handle.Status == AsyncOperationStatus.Succeeded)
					return bindObj.Handles[path].Convert<T>().Result;
			}
			handle = Addressables.LoadAssetAsync<T>(path);
			bindObj.Handles[path] = handle;
			return handle.WaitForCompletion();
		}

		/// <summary>
		/// 以 AssetReference 同步读取资源，当绑定的 HomoBehaviour 实例或实现了 IOnDestroyAssetLoad 接口的 MonoBehaviour 类被销毁时，会自动释放引用计数。
		/// </summary>
		/// <param name="bindObj">绑定的 HomoBehaviour 实例</param>
		/// <param name="reference">读取的 AssetReference</param>
		/// <typeparam name="T">读取的资源的类型</typeparam>
		/// <returns></returns>
		public static T LoadAsset<T>(this IOnDestroyAssetLoad bindObj, AssetReference reference) {

			if (bindObj.Handles == null) {
				bindObj.Handles = new Dictionary<string, AsyncOperationHandle>();
				bindObj.OnDestroyEvent += Release;
			}
			AsyncOperationHandle<T> handle;
			if (bindObj.Handles.ContainsKey(reference.ToString())) {
				handle = bindObj.Handles[reference.ToString()].Convert<T>();
				if (handle.Status == AsyncOperationStatus.None)
					handle.WaitForCompletion();
				if (handle.Status == AsyncOperationStatus.Succeeded)
					return bindObj.Handles[reference.ToString()].Convert<T>().Result;
			}
			handle = reference.LoadAssetAsync<T>();
			bindObj.Handles[reference.ToString()] = handle;
			return handle.WaitForCompletion();
		}
		/// <summary>
		/// 以 Addressable 路径异步读取资源，当绑定的 HomoBehaviour 实例或实现了 IOnDestroyAssetLoad 接口的 MonoBehaviour 类被销毁时，会自动释放引用计数。
		/// </summary>
		/// <param name="bindObj">绑定的 HomoBehaviour 实例</param>
		/// <param name="path">读取的 Addressable 路径，默认情况下就是项目中的相对路径</param>
		/// <param name="progress">可以填写 Progress 实例,将会以读取进度作为参数执行该实例中的委托 </param>
		/// <param name="timing">执行异步的时间点</param>
		/// <param name="token">取消信号</param>
		/// <typeparam name="T">读取的资源的类型</typeparam>
		/// <returns></returns>
		/// <exception cref="OperationCanceledException"></exception>
		public static async UniTask<T> LoadAssetAsync<T>(this IOnDestroyAssetLoad bindObj, string path, IProgress<float> progress = null,
			PlayerLoopTiming timing = PlayerLoopTiming.Update,
			CancellationToken token = default) {

			if (token == default) {
				token = (bindObj as MonoBehaviour).GetCancellationTokenOnDestroy();
			}

			if (token.IsCancellationRequested) throw new OperationCanceledException();

			if (bindObj.Handles == null) {
				bindObj.Handles = new Dictionary<string, AsyncOperationHandle>();
				bindObj.OnDestroyEvent += Release;
			}

			AsyncOperationHandle<T> handle;
			if (bindObj.Handles.ContainsKey(path)) {
				handle = bindObj.Handles[path].Convert<T>();
				if (handle.IsDone) return handle.Result;
				return await handle.ToUniTask(progress, timing, token);
			}

			handle = Addressables.LoadAssetAsync<T>(path);
			bindObj.Handles[path] = handle;
			return await handle.ToUniTask(progress, timing, token);
		}
		/// <summary>
		/// 以 AssetReference 异步读取资源，当绑定的 HomoBehaviour 实例或实现了 IOnDestroyAssetLoad 接口的 MonoBehaviour 类被销毁时，会自动释放引用计数。
		/// </summary>
		/// <param name="bindObj">绑定的 HomoBehaviour 实例</param>
		/// <param name="reference">读取的 AssetReference</param>
		/// <param name="progress">可以填写 Progress 实例,将会以读取进度作为参数执行该实例中的委托 </param>
		/// <param name="timing">执行异步的时间点</param>
		/// <param name="token">取消信号</param>
		/// <typeparam name="T">读取的资源的类型</typeparam>
		/// <returns></returns>
		public static async UniTask<T> LoadAssetAsync<T>(this IOnDestroyAssetLoad bindObj, AssetReference reference, IProgress<float> progress = null,
			PlayerLoopTiming timing = PlayerLoopTiming.Update,
			CancellationToken token = default) {

			if (token == default) {
				token = (bindObj as MonoBehaviour).GetCancellationTokenOnDestroy();
			}

			if (token.IsCancellationRequested) throw new OperationCanceledException();

			if (bindObj.Handles == null) {
				bindObj.Handles = new Dictionary<string, AsyncOperationHandle>();
				bindObj.OnDestroyEvent += Release;
			}

			AsyncOperationHandle<T> handle;
			if (bindObj.Handles.ContainsKey(reference.ToString())) {
				handle = bindObj.Handles[reference.ToString()].Convert<T>();
				if (handle.IsDone) return handle.Result;
				return await handle.ToUniTask(progress, timing, token);
			}

			handle = reference.LoadAssetAsync<T>();
			bindObj.Handles[reference.ToString()] = handle;
			return await handle.ToUniTask(progress, timing, token);
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
		/// <returns></returns>
		public static GameObject Instantiate(string path, Transform parent = null) {
			var handle = Addressables.InstantiateAsync(path,parent);
			var gameObject = handle.WaitForCompletion();
			return gameObject;

		}

		/// <summary>
		/// 以 AssetReferenceGameObject 同步读取预制体的依赖资源并实例化之。
		/// </summary>
		/// <param name="reference">读取的 AssetReferenceGameObject</param>
		/// <param name="parent">实例化在场景中的父节点</param>
		/// <returns></returns>
		public static GameObject Instantiate(AssetReferenceGameObject reference, Transform parent = null) {
			var handle = reference.InstantiateAsync(parent);
			var gameObject = handle.WaitForCompletion();
			return gameObject;
		}

		/// <summary>
		/// 以 Addressable 路径异步读取预制体的依赖资源并实例化之。
		/// </summary>
		/// <param name="path">读取的 Addressable 路径，默认情况下就是项目中的相对路径</param>
		/// <param name="parent">实例化在场景中的父节点</param>
		/// <param name="progress">可以填写 Progress 实例,将会以读取进度作为参数执行该实例中的委托 </param>
		/// <param name="timing">执行异步的时间点</param>
		/// <param name="token">取消信号</param>
		/// <returns></returns>
		public static async UniTask<GameObject> InstantiateAsync(string path, Transform parent = null, IProgress<float> progress = null,
			PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken token = default) {

			var handle = Addressables.InstantiateAsync(path,parent);
			var gameObject = await handle.ToUniTask(progress, timing, token);
			return gameObject;
		}

		/// <summary>
		/// 以 AssetReferenceGameObject 异步读取预制体的依赖资源并实例化之。
		/// </summary>
		/// <param name="reference">读取的 AssetReferenceGameObject</param>
		/// <param name="parent">实例化在场景中的父节点</param>
		/// <param name="progress">可以填写 Progress 实例,将会以读取进度作为参数执行该实例中的委托 </param>
		/// <param name="timing">执行异步的时间点</param>
		/// <param name="token">取消信号</param>
		/// <returns></returns>
		public static async UniTask<GameObject> InstantiateAsync(AssetReferenceGameObject reference, Transform parent = null, IProgress<float> progress = null,
			PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken token = default) {

			var handle = reference.InstantiateAsync(parent);
			var result = await handle.ToUniTask(progress, timing, token);
			return result;
		}

		/// <summary>
		/// 以 Addressable 路径同步读取场景实例及其依赖资源，也可以自动加载。。
		/// </summary>
		/// <param name="path">读取的 Addressable 路径，默认情况下就是项目中的相对路径</param>
		/// <param name="mode">加载场景的方式，Single 会卸载当前场景，Additive 则会保留</param>
		/// <param name="activeOnLoad">为 true 时，读取后会自动加载</param>
		/// <returns></returns>
		public static SceneInstance LoadScene(string path, LoadSceneMode mode = LoadSceneMode.Single, bool activeOnLoad = true) {
			var handle = Addressables.LoadSceneAsync(path, mode, activeOnLoad);
			return handle.WaitForCompletion();
		}

		/// <summary>
		/// 以 AssetReference 同步读取场景实例及其依赖资源，也可以自动加载。。
		/// </summary>
		/// <param name="reference">读取的 AssetReference</param>
		/// <param name="mode">加载场景的方式，Single 会卸载当前场景，Additive 则会保留</param>
		/// <param name="activeOnLoad">为 true 时，读取后会自动加载</param>
		/// <returns></returns>
		public static SceneInstance LoadScene(AssetReference reference, LoadSceneMode mode = LoadSceneMode.Single, bool activeOnLoad = true) {
			var handle = reference.LoadSceneAsync(mode, activeOnLoad);
			return handle.WaitForCompletion();
		}

		/// <summary>
		/// 以 Addressable 路径异步读取场景实例及其依赖资源，也可以自动加载。
		/// </summary>
		/// <param name="path">读取的 Addressable 路径，默认情况下就是项目中的相对路径</param>
		/// <param name="mode">加载场景的方式，Single 会卸载当前场景，Additive 则会保留</param>
		/// <param name="activeOnLoad">为 true 时，读取后会自动加载</param>
		/// <param name="progress">可以填写 Progress 实例,将会以读取进度作为参数执行该实例中的委托 </param>
		/// <param name="timing">执行异步的时间点</param>
		/// <param name="token">取消信号</param>
		/// <returns></returns>
		public static async UniTask<SceneInstance> LoadSceneAsync(string path, LoadSceneMode mode = LoadSceneMode.Single, bool activeOnLoad = true, IProgress<float> progress = null,
			PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken token = default) {
			var handle = Addressables.LoadSceneAsync(path, mode, activeOnLoad);
			return await handle.ToUniTask(progress, timing, token);
		}

		/// <summary>
		/// 以 AssetReference 异步读取场景实例及其依赖资源，也可以自动加载。
		/// </summary>
		/// <param name="reference">读取的 AssetReference</param>
		/// <param name="mode">加载场景的方式，Single 会卸载当前场景，Additive 则会保留</param>
		/// <param name="activeOnLoad">为 true 时，读取后会自动加载</param>
		/// <param name="progress">可以填写 Progress 实例,将会以读取进度作为参数执行该实例中的委托 </param>
		/// <param name="timing">执行异步的时间点</param>
		/// <param name="token">取消信号</param>
		public static async UniTask<SceneInstance> LoadSceneAsync(AssetReference reference, LoadSceneMode mode = LoadSceneMode.Single, bool activeOnLoad = true, IProgress<float> progress = null,
			PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken token = default) {
			var handle = reference.LoadSceneAsync(mode, activeOnLoad);
			return await handle.ToUniTask(progress, timing, token);
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
		/// <param name="sceneInstance"></param>
		public static async UniTaskVoid ReleaseScene(this SceneInstance sceneInstance) {
			await Addressables.UnloadSceneAsync(sceneInstance);
		}

	}

}