using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace HomoAddressableTools {
	public interface IOnDestroy {
		event OnDestroyDelegate OnDestroyEvent;
	}

	public interface IOnDestroyAssetLoad: IOnDestroy {
		Dictionary<string,AsyncOperationHandle> Handles { get; set; }
	}
	
}