

using System;
using System.Collections.Generic;
using FixedMath;
using Unity.Entities;





public struct Agent : IComponentData
{
    public Entity entity;
    public bool reachDestination;
    public FixedVector2 position_;
    public FixedVector2 prefVelocity_;
    public FixedVector2 faceTo_;
    public FixedVector2 velocity_;
    public int id_;
    public int maxNeighbors_;
    public FixedInt maxSpeed_;
    public FixedInt neighborDist_;
    public FixedInt radius_;
    public FixedInt timeHorizon_;
    public FixedInt timeHorizonObst_;
    public FixedVector2 newVelocity_;
    public bool needDelete_;

    public int faction_;
 

 
 


}


public struct AgentTreeNode
{
    public int begin_;
    public int end_;
    public int left_;
    public int right_;
    public FixedInt maxX_;
    public FixedInt maxY_;
    public FixedInt minX_;
    public FixedInt minY_;
}
