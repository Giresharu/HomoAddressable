using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace HomoAddressableTools {
	public interface IOnDestroy {
		event OnDestroyDelegate OnDestroyEvent;
	}

	public interface IOnDestroyAssetLoad: IOnDestroy {
		Dictionary<string,AsyncOperationHandle<Object>> Handles { get; set; }
	}
}