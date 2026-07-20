using UnityEngine;

public class NormalBall : Ball
{


    protected override bool IsNormalBall => true;

    protected override void Awake()
    {
        base.Awake();

        type = Object_Type.Ball_Normal;
    }

 


}
