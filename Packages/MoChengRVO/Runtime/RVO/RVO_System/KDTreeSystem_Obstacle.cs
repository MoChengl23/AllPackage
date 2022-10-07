using System.Collections;
using System.Collections.Generic;
using FixedMath;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;

public partial class KDTreeSystem
{

    public NativeList<Obstacle> obstacles_ = new NativeList<Obstacle>(Allocator.Persistent);

    public NativeList<ObstacleTreeNode> obstacleTree_ = new NativeList<ObstacleTreeNode>(Allocator.Persistent);



    [BurstCompile]
    private struct BuildObstacleTreeJob : IJob
    {
        public NativeList<Obstacle> obstacles_;
        public NativeList<ObstacleTreeNode> obstacleTree_;
        public void Execute()
        {
            BuildObstacleTree(0, obstacles_.Length, 0);
        }

        private void BuildObstacleTree(int begin, int end, int node)
        {
            if (obstacles_.Length == 0) return;
            var treeNode = obstacleTree_[node];
            treeNode.begin_ = begin;
            treeNode.end_ = end;
            treeNode.minX_ = treeNode.maxX_ = obstacles_[begin].position_.X;
            treeNode.minY_ = treeNode.maxY_ = obstacles_[begin].position_.Y;
            obstacleTree_[node] = treeNode;
            for (int i = begin + 1; i < end; ++i)
            {
                treeNode.maxX_ = FixedCalculate.Max(obstacleTree_[node].maxX_, obstacles_[i].position_.X);
                treeNode.minX_ = FixedCalculate.Min(obstacleTree_[node].minX_, obstacles_[i].position_.X);
                treeNode.maxY_ = FixedCalculate.Max(obstacleTree_[node].maxY_, obstacles_[i].position_.Y);
                treeNode.minY_ = FixedCalculate.Min(obstacleTree_[node].minY_, obstacles_[i].position_.Y);

                obstacleTree_[node] = treeNode;
            }
            if (end - begin > MAX_LEAF_SIZE)
            {
                /* No leaf node. */
                bool isVertical = obstacleTree_[node].maxX_ - obstacleTree_[node].minX_ > obstacleTree_[node].maxY_ - obstacleTree_[node].minY_;
                FixedInt splitValue = FixedInt.half * (isVertical ? obstacleTree_[node].maxX_ + obstacleTree_[node].minX_ : obstacleTree_[node].maxY_ + obstacleTree_[node].minY_);

                int left = begin;
                int right = end;

                while (left < right)
                {
                    while (left < right && (isVertical ? obstacles_[left].position_.X : obstacles_[left].position_.Y) < splitValue)
                    {
                        ++left;
                    }

                    while (right > left && (isVertical ? obstacles_[right - 1].position_.X : obstacles_[right - 1].position_.Y) >= splitValue)
                    {
                        --right;
                    }

                    if (left < right)
                    {
                        Obstacle obstacle = obstacles_[left];
                        obstacles_[left] = obstacles_[right - 1];
                        obstacles_[right - 1] = obstacle;
                        ++left;
                        --right;
                    }
                }
                int leftSize = left - begin;

                if (leftSize == 0)
                {
                    ++leftSize;
                    ++left;
                    ++right;
                }
                treeNode.left_ = node + 1;
                treeNode.right_ = node + 2 * leftSize;
                obstacleTree_[node] = treeNode;

                // interactableObjectTree_[node].left_ = node + 1;
                // interactableObjectTree_[node].right_ = node + 2 * leftSize;

                // BuildAgentTree(interactableObjects_, interactableObjectTree_, begin, left, interactableObjectTree_[node].left_);
                // BuildAgentTree(interactableObjects_, interactableObjectTree_, left, end, interactableObjectTree_[node].right_);
                BuildObstacleTree(begin, left, obstacleTree_[node].left_);
                BuildObstacleTree(left, end, obstacleTree_[node].right_);
            }


        }


    }




}
