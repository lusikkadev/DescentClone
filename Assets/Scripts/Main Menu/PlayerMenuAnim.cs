using UnityEngine;

public class PlayerMenuAnim : MonoBehaviour
{
    Animator animator;
    AnimationClip idleAnim;
    AnimationClip runAnim;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void FlyOut()
    {
        animator.SetTrigger("FlyOut");
    }
}
