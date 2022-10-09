using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace HomoAddressableTools {
	public abstract class HomoBehaviour: MonoBehaviour, IOnDestroyAssetLoad {

		public event OnDestroyDelegate OnDestroyEvent;
		public Dictionary<string, AsyncOperationHandle> Handles { get; set; }

		RefCount refCount = new RefCount();

		protected void OnDestroy() {
			OnDestroyInvoke();
		}

		void OnDestroyInvoke() {
			refCount.Count--;
			if (refCount.Count <= 0)
				OnDestroyEvent?.Invoke(this);
		}

		public GameObject Clone(Transform parent = null) {
			refCount.Count++;
			var obj = Instantiate(gameObject, parent);
			obj.GetComponent<HomoBehaviour>().refCount = refCount;
			return obj;

		}

	}

	class RefCount {
		public int Count { get; set; }
	}

}