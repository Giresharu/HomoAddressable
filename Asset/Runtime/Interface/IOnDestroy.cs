using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace HomoAddressableTools {
	public interface IOnDestroy {
		event OnDestroyDelegate OnDestroyEvent;
	}

	public interface IOnDestroyAssetLoad: IOnDestroy {

		List<string> Keys { get; set; }
		
		Dictionary<string,AsyncOperationHandle<Object>> Handles { get; set; }
	}
}