// Extension method for cleaner coroutine starting
using System.Collections;
using UnityEngine;

public static class CoroutineExtensions {
    public static Coroutine AsCoroutine(this IEnumerator coroutine, MonoBehaviour coroutineOwner) {
        return coroutineOwner.StartCoroutine(coroutine);
    }

   // public static Coroutine AsCoroutine(this IEnumerator coroutine) {
   //     // Assumes coroutine is being started from a MonoBehaviour context.
   //     // Consider error handling if there's no suitable MonoBehaviour to attach to.
   //     // For this example, we'll assume GameManager.Instance exists and is a MonoBehaviour.
   //     if (GameManager.Instance != null) {
   //         return GameManager.Instance.StartCoroutine(coroutine);
   //     } else {
   //         Debug.LogError("No GameManager instance found to start coroutine from.");
   //         return null; // Or throw an exception depending on your error handling needs.
   //     }
   // }

    public static IEnumerator AsIEnumerator(this Coroutine coroutine) {
        yield return coroutine;
    }

    public static IEnumerator AsIEnumerator(this AsyncOperation asyncOp) {
        while (!asyncOp.isDone) {
            yield return null;
        }
    }
}