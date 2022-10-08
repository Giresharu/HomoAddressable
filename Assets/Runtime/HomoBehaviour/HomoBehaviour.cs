using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace HomoAddressableTools {
	public abstract class HomoBehaviour: MonoBehaviour, IOnDestroyAssetLoad {
		public event OnDestroyDelegate OnDestroyEvent;
		public Dictionary<string, AsyncOperationHandle<Object>> Handles { get; set; }

		protected void OnDestroy() {
			OnDestroyInvoke();
		}

		void OnDestroyInvoke() {
			OnDestroyEvent?.Invoke(this);
		}
	}
}