using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using FixedMath;
using UnityEngine;
using System;




public partial class KDTreeSystem
{
 
 

    [BurstCompile]
    public struct UpdateAgentJobParallel : IJobParallelFor
    {
        
      
        private Entity entity;
       
        private Agent agent;
        [NativeDisableContainerSafetyRestriction]
        public NativeList<Agent> agents;
        [ReadOnly]
        public NativeList<AgentTreeNode> agentTree;

        [ReadOnly]
        public NativeList<ObstacleVertice> obstacleVertices_;
        [ReadOnly]
        public NativeList<ObstacleVerticeTreeNode> obstacleVerticesTree_;
        [ReadOnly]
        public ObstacleVerticeTreeNode obstacleVerticesTreeRoot;

        [NativeDisableContainerSafetyRestriction]
        public EntityCommandBuffer.ParallelWriter ecbPara;

        public void Execute(int index)
        {

            agent = agents[index];
            entity = agent.entity;
            #region  Compute Agent Neighbor
            NativeList<AgentNeighbor> agentNeighbors = new NativeList<AgentNeighbor>(Allocator.Temp);
            var rangeSq = FixedCalculate.Square(agent.neighborDist_);

            ComputeAgentNeighbor(ref rangeSq, 0, agentNeighbors);
            #endregion

            #region  Compute Obstacle Neighbor

            NativeList<ObstacleVerticeNeighbor> obstacleNeighbors = new NativeList<ObstacleVerticeNeighbor>(Allocator.Temp);
            var obstacleRangeSq = FixedCalculate.Square(agent.timeHorizonObst_ * agent.maxSpeed_ + agent.radius_);
            ComputeObstacleNeighbor(obstacleVerticesTreeRoot, obstacleRangeSq, obstacleNeighbors);

            #endregion

            NativeList<Line> orcaLines = new NativeList<Line>(Allocator.Temp);
            AddObstacleLine(agent, orcaLines, obstacleNeighbors, obstacleVertices_);
            AddAgentLine(orcaLines, agentNeighbors);



          
            agent.velocity_ = agent.newVelocity_;
            agent.position_ += agent.newVelocity_ / 5;


            ecbPara.SetComponent<Agent>(index, entity, agent);
            

            agentNeighbors.Dispose();
            obstacleNeighbors.Dispose();
            orcaLines.Dispose();



        }
        private void ComputeAgentNeighbor(ref FixedInt rangeSq, int node, NativeList<AgentNeighbor> agentNeighbors)
        {

            if (agentTree[node].end_ - agentTree[node].begin_ <= MAX_LEAF_SIZE)
            {
                for (int i = agentTree[node].begin_; i < agentTree[node].end_; ++i)
                {
                    InsertAgentNeighbor(agent, agents[i], ref rangeSq, agentNeighbors);
                }
            }
            else
            {
                FixedInt distSqLeft = FixedCalculate.Square(FixedCalculate.Max(0, agentTree[agentTree[node].left_].minX_ - agent.position_.X)) + FixedCalculate.Square(FixedCalculate.Max(0, agent.position_.X - agentTree[agentTree[node].left_].maxX_)) + FixedCalculate.Square(FixedCalculate.Max(0, agentTree[agentTree[node].left_].minY_ - agent.position_.Y)) + FixedCalculate.Square(FixedCalculate.Max(0, agent.position_.Y - agentTree[agentTree[node].left_].maxY_));
                FixedInt distSqRight = FixedCalculate.Square(FixedCalculate.Max(0, agentTree[agentTree[node].right_].minX_ - agent.position_.X)) + FixedCalculate.Square(FixedCalculate.Max(0, agent.position_.X - agentTree[agentTree[node].right_].maxX_)) + FixedCalculate.Square(FixedCalculate.Max(0, agentTree[agentTree[node].right_].minY_ - agent.position_.Y)) + FixedCalculate.Square(FixedCalculate.Max(0, agent.position_.Y - agentTree[agentTree[node].right_].maxY_));

                if (distSqLeft < distSqRight)
                {
                    if (distSqLeft < rangeSq)
                    {
                        ComputeAgentNeighbor(ref rangeSq, agentTree[node].left_, agentNeighbors);

                        if (distSqRight < rangeSq)
                        {
                            ComputeAgentNeighbor(ref rangeSq, agentTree[node].right_, agentNeighbors);
                        }
                    }
                }
                else
                {
                    if (distSqRight < rangeSq)
                    {
                        ComputeAgentNeighbor(ref rangeSq, agentTree[node].right_, agentNeighbors);

                        if (distSqLeft < rangeSq)
                        {
                            ComputeAgentNeighbor(ref rangeSq, agentTree[node].left_, agentNeighbors);
                        }
                    }
                }

            }
        }
        private void InsertAgentNeighbor(Agent agent, Agent neighbor, ref FixedInt rangeSq, NativeList<AgentNeighbor> agentNeighbors_)
        {
            if (agent.id_ == neighbor.id_) return;
            FixedInt distSq = FixedCalculate.Square(agent.position_ - neighbor.position_);

            if (distSq < rangeSq)
            {
                if (agentNeighbors_.Length < agent.maxNeighbors_)
                {
                    agentNeighbors_.Add(new AgentNeighbor { distance = distSq, agent = neighbor });
                }

                int i = agentNeighbors_.Length - 1;

                while (i != 0 && distSq < agentNeighbors_[i - 1].distance)
                {
                    agentNeighbors_[i] = agentNeighbors_[i - 1];
                    --i;
                }

                agentNeighbors_[i] = new AgentNeighbor { distance = distSq, agent = neighbor };

                if (agentNeighbors_.Length == agent.maxNeighbors_)
                {
                    rangeSq = agentNeighbors_[agentNeighbors_.Length - 1].distance;
                }
            }

        }


        private void ComputeObstacleNeighbor(ObstacleVerticeTreeNode node, FixedInt rangeSq, NativeList<ObstacleVerticeNeighbor> obstacleNeighbors)
        {
            if (node.obstacleVertice_Index == -1) return;
            ObstacleVertice obstacle1 = obstacleVertices_[node.obstacleVertice_Index];
            ObstacleVertice obstacle2 = obstacleVertices_[obstacle1.next_];

            FixedInt agentLeftOfLine = FixedCalculate.LeftOf(obstacle1.point_, obstacle2.point_, agent.position_);

            if (agentLeftOfLine >= 0)
            {
                if (node.left_index != -1) ComputeObstacleNeighbor(obstacleVerticesTree_[node.left_index], rangeSq, obstacleNeighbors);
            }
            else
            {
                if (node.right_index != -1) ComputeObstacleNeighbor(obstacleVerticesTree_[node.right_index], rangeSq, obstacleNeighbors);
            }
            // ComputeObstacleNeighbor(obstacles,obstacleTree, agentLeftOfLine >= 0 ? obstacleTree[node.left_index] : obstacleTree[node.right_index]  , agent, ref rangeSq, obstacleNeighbors);

            FixedInt distSqLine = FixedCalculate.Square(agentLeftOfLine) / FixedCalculate.Square(obstacle2.point_ - obstacle1.point_);

            if (distSqLine < rangeSq)
            {
                if (agentLeftOfLine < 0)
                {
                    /*
                        * Try obstacle at this node only if agent is on right side of
                        * obstacle (and can see obstacle).
                        */
                    InsertObstacleNeighbor(obstacle1, obstacleNeighbors, rangeSq);
                    // agent.insertObstacleNeighbor(node.obstacle_, rangeSq);
                }

                /* Try other side of line. */
                if (agentLeftOfLine >= 0)
                {
                    if (node.right_index != -1) ComputeObstacleNeighbor(obstacleVerticesTree_[node.right_index], rangeSq, obstacleNeighbors);
                }
                else
                {
                    if (node.left_index != -1) ComputeObstacleNeighbor(obstacleVerticesTree_[node.left_index], rangeSq, obstacleNeighbors);
                }

            }

        }

        private void InsertObstacleNeighbor(ObstacleVertice obstacle, NativeList<ObstacleVerticeNeighbor> obstacleNeighbors_, FixedInt rangeSq)
        {
            ObstacleVertice nextObstacle = obstacleVertices_[obstacle.next_];

            FixedInt distSq = FixedCalculate.DistSqPointLineSegment(obstacle.point_, nextObstacle.point_, agent.position_);

            if (distSq < rangeSq)
            {
                obstacleNeighbors_.Add(new ObstacleVerticeNeighbor { distance = distSq, obstacle = obstacle });

                int i = obstacleNeighbors_.Length - 1;

                while (i != 0 && distSq < obstacleNeighbors_[i - 1].distance)
                {
                    obstacleNeighbors_[i] = obstacleNeighbors_[i - 1];
                    --i;
                }
                obstacleNeighbors_[i] = new ObstacleVerticeNeighbor { distance = distSq, obstacle = obstacle };
            }

        }




        private void AddAgentLine(NativeList<Line> orcaLines_, NativeList<AgentNeighbor> agentNeighbors_)
        {
            int numObstLines = orcaLines_.Length;
            FixedInt invTimeHorizon = 1 / agent.timeHorizon_;
            /* Create agent ORCA lines. */
            for (int i = 0; i < agentNeighbors_.Length; ++i)
            {
                Agent other = agentNeighbors_[i].agent;

                FixedVector2 relativePosition = other.position_ - agent.position_;
                FixedVector2 relativeVelocity = agent.velocity_ - other.velocity_;
                FixedInt distSq = FixedCalculate.Square(relativePosition);
                FixedInt combinedRadius = agent.radius_ + other.radius_;
                FixedInt combinedRadiusSq = FixedCalculate.Square(combinedRadius);

                Line line;
                FixedVector2 u;

                if (distSq > combinedRadiusSq)
                {

                    /* No collision. */
                    FixedVector2 w = relativeVelocity - invTimeHorizon * relativePosition;

                    /* Vector from cutoff center to relative velocity. */
                    FixedInt wLengthSq = FixedCalculate.Square(w);
                    FixedInt dotProduct1 = w * relativePosition;

                    if (dotProduct1 < 0 && FixedCalculate.Square(dotProduct1) > combinedRadiusSq * wLengthSq)
                    {
                        /* Project on cut-off circle. */
                        FixedInt wLength = FixedCalculate.Sqrt(wLengthSq);
                        FixedVector2 unitW = w / wLength;

                        line.direction = new FixedVector2(unitW.Y, -unitW.X);
                        u = (combinedRadius * invTimeHorizon - wLength) * unitW;
                    }
                    else
                    {
                        /* Project on legs. */
                        FixedInt leg = FixedCalculate.Sqrt(distSq - combinedRadiusSq);

                        if (FixedCalculate.Det(relativePosition, w) > 0)
                        {
                            /* Project on left leg. */
                            line.direction = new FixedVector2(relativePosition.X * leg - relativePosition.Y * combinedRadius, relativePosition.X * combinedRadius + relativePosition.Y * leg) / distSq;
                        }
                        else
                        {
                            /* Project on right leg. */
                            line.direction = -new FixedVector2(relativePosition.X * leg + relativePosition.Y * combinedRadius, -relativePosition.X * combinedRadius + relativePosition.Y * leg) / distSq;
                        }

                        FixedInt dotProduct2 = relativeVelocity * line.direction;
                        u = dotProduct2 * line.direction - relativeVelocity;
                    }
                }
                else
                {
                    /* Collision. Project on cut-off circle of time timeStep. */
                    FixedInt invTimeStep = 15;

                    /* Vector from cutoff center to relative velocity. */
                    FixedVector2 w = relativeVelocity - invTimeStep * relativePosition;


                    FixedInt wLength = FixedCalculate.Abs(w);
                    FixedVector2 unitW = w / wLength;

                    line.direction = new FixedVector2(unitW.Y, -unitW.X);
                    u = (combinedRadius * invTimeStep - wLength) * unitW;
                }

                line.point = agent.velocity_ + FixedInt.half * u;
                orcaLines_.Add(line);
            }
            int lineFail = linearProgram2(orcaLines_, agent.maxSpeed_, agent.prefVelocity_, false, ref agent.newVelocity_);

            if (lineFail < orcaLines_.Length)
            {
                linearProgram3(orcaLines_, numObstLines, lineFail, agent.maxSpeed_, ref agent.newVelocity_);
            }

        }

        private void AddObstacleLine(Agent agent, NativeList<Line> orcaLines_, NativeList<ObstacleVerticeNeighbor> obstacleNeighbors_, NativeList<ObstacleVertice> obstacles)
        {
            FixedInt invTimeHorizonObst = 1 / agent.timeHorizonObst_;
            var radius_ = agent.radius_;
            var velocity_ = agent.velocity_;
            var position_ = agent.position_;

            /* Create obstacle ORCA lines. */
            for (int i = 0; i < obstacleNeighbors_.Length; ++i)
            {

                ObstacleVertice obstacle1 = obstacleNeighbors_[i].obstacle;
                ObstacleVertice obstacle2 = obstacles[obstacle1.next_];

                FixedVector2 relativePosition1 = obstacle1.point_ - agent.position_;
                FixedVector2 relativePosition2 = obstacle2.point_ - agent.position_;

                /*
                * Check if velocity obstacle of obstacle is already taken care
                * of by previously constructed obstacle ORCA lines.
                */
                bool alreadyCovered = false;

                for (int j = 0; j < orcaLines_.Length; ++j)
                {
                    if (FixedCalculate.Det(invTimeHorizonObst * relativePosition1 - orcaLines_[j].point, orcaLines_[j].direction) - invTimeHorizonObst * radius_ >= -(FixedInt)(long)1 << 5
                    && FixedCalculate.Det(invTimeHorizonObst * relativePosition2 - orcaLines_[j].point, orcaLines_[j].direction) - invTimeHorizonObst * radius_ >= -(FixedInt)(long)1 << 5)
                    {
                        alreadyCovered = true;

                        break;
                    }
                }

                if (alreadyCovered)
                {
                    continue;
                }

                /* Not yet covered. Check for collisions. */
                FixedInt distSq1 = FixedCalculate.Square(relativePosition1);
                FixedInt distSq2 = FixedCalculate.Square(relativePosition2);

                FixedInt radiusSq = FixedCalculate.Square(radius_);

                FixedVector2 obstacleVector = obstacle2.point_ - obstacle1.point_;
                FixedInt s = (-relativePosition1 * obstacleVector) / FixedCalculate.Square(obstacleVector);
                FixedInt distSqLine = FixedCalculate.Square(-relativePosition1 - s * obstacleVector);

                Line line;

                if (s < 0 && distSq1 <= radiusSq)
                {
                    /* Collision with left vertex. Ignore if non-convex. */
                    if (obstacle1.convex_)
                    {
                        line.point = new FixedVector2(0, 0);
                        line.direction = FixedCalculate.Normalize(new FixedVector2(-relativePosition1.Y, relativePosition1.X));
                        orcaLines_.Add(line);
                    }

                    continue;
                }
                else if (s > 1 && distSq2 <= radiusSq)
                {
                    /*
                    * Collision with right vertex. Ignore if non-convex or if
                    * it will be taken care of by neighboring obstacle.
                    */
                    if (obstacle2.convex_ && FixedCalculate.Det(relativePosition2, obstacle2.direction_) >= 0)
                    {
                        line.point = new FixedVector2(0, 0);
                        line.direction = FixedCalculate.Normalize(new FixedVector2(-relativePosition2.Y, relativePosition2.X));
                        orcaLines_.Add(line);
                    }

                    continue;
                }
                else if (s >= 0 && s < 1 && distSqLine <= radiusSq)
                {
                    /* Collision with obstacle segment. */
                    line.point = new FixedVector2(0, 0);
                    line.direction = -obstacle1.direction_;
                    orcaLines_.Add(line);

                    continue;
                }

                /*
                * No collision. Compute legs. When obliquely viewed, both legs
                * can come from a single vertex. Legs extend cut-off line when
                * non-convex vertex.
                */

                FixedVector2 leftLegDirection, rightLegDirection;

                if (s < 0 && distSqLine <= radiusSq)
                {
                    /*
                    * Obstacle viewed obliquely so that left vertex
                    * defines velocity obstacle.
                    */
                    if (!obstacle1.convex_)
                    {
                        /* Ignore obstacle. */
                        continue;
                    }

                    obstacle2 = obstacle1;

                    FixedInt leg1 = FixedCalculate.Sqrt(distSq1 - radiusSq);
                    leftLegDirection = new FixedVector2(relativePosition1.X * leg1 - relativePosition1.Y * radius_, relativePosition1.X * radius_ + relativePosition1.Y * leg1) / distSq1;
                    rightLegDirection = new FixedVector2(relativePosition1.X * leg1 + relativePosition1.Y * radius_, -relativePosition1.X * radius_ + relativePosition1.Y * leg1) / distSq1;
                }
                else if (s > 1 && distSqLine <= radiusSq)
                {
                    /*
                    * Obstacle viewed obliquely so that
                    * right vertex defines velocity obstacle.
                    */
                    if (!obstacle2.convex_)
                    {
                        /* Ignore obstacle. */
                        continue;
                    }

                    obstacle1 = obstacle2;

                    FixedInt leg2 = FixedCalculate.Sqrt(distSq2 - radiusSq);
                    leftLegDirection = new FixedVector2(relativePosition2.X * leg2 - relativePosition2.Y * radius_, relativePosition2.X * radius_ + relativePosition2.Y * leg2) / distSq2;
                    rightLegDirection = new FixedVector2(relativePosition2.X * leg2 + relativePosition2.Y * radius_, -relativePosition2.X * radius_ + relativePosition2.Y * leg2) / distSq2;
                }
                else
                {
                    /* Usual situation. */
                    if (obstacle1.convex_)
                    {
                        FixedInt leg1 = FixedCalculate.Sqrt(distSq1 - radiusSq);
                        leftLegDirection = new FixedVector2(relativePosition1.X * leg1 - relativePosition1.Y * radius_, relativePosition1.X * radius_ + relativePosition1.Y * leg1) / distSq1;
                    }
                    else
                    {
                        /* Left vertex non-convex; left leg extends cut-off line. */
                        leftLegDirection = -obstacle1.direction_;
                    }

                    if (obstacle2.convex_)
                    {
                        FixedInt leg2 = FixedCalculate.Sqrt(distSq2 - radiusSq);
                        rightLegDirection = new FixedVector2(relativePosition2.X * leg2 + relativePosition2.Y * radius_, -relativePosition2.X * radius_ + relativePosition2.Y * leg2) / distSq2;
                    }
                    else
                    {
                        /* Right vertex non-convex; right leg extends cut-off line. */
                        rightLegDirection = obstacle1.direction_;
                    }
                }

                /*
                * Legs can never point into neighboring edge when convex
                * vertex, take cutoff-line of neighboring edge instead. If
                * velocity projected on "foreign" leg, no constraint is added.
                */

                ObstacleVertice leftNeighbor = obstacles[obstacle1.previous_];

                bool isLeftLegForeign = false;
                bool isRightLegForeign = false;

                if (obstacle1.convex_ && FixedCalculate.Det(leftLegDirection, -leftNeighbor.direction_) >= 0)
                {
                    /* Left leg points into obstacle. */
                    leftLegDirection = -leftNeighbor.direction_;
                    isLeftLegForeign = true;
                }

                if (obstacle2.convex_ && FixedCalculate.Det(rightLegDirection, obstacle2.direction_) <= 0)
                {
                    /* Right leg points into obstacle. */
                    rightLegDirection = obstacle2.direction_;
                    isRightLegForeign = true;
                }

                /* Compute cut-off centers. */
                FixedVector2 leftCutOff = invTimeHorizonObst * (obstacle1.point_ - position_);
                FixedVector2 rightCutOff = invTimeHorizonObst * (obstacle2.point_ - position_);
                FixedVector2 cutOffVector = rightCutOff - leftCutOff;

                /* Project current velocity on velocity obstacle. */

                /* Check if current velocity is projected on cutoff circles. */
                FixedInt t = obstacle1 == obstacle2 ? FixedInt.half : ((velocity_ - leftCutOff) * cutOffVector) / FixedCalculate.Square(cutOffVector);
                FixedInt tLeft = (velocity_ - leftCutOff) * leftLegDirection;
                FixedInt tRight = (velocity_ - rightCutOff) * rightLegDirection;

                if ((t < 0 && tLeft < 0) || (obstacle1 == obstacle2 && tLeft < 0 && tRight < 0))
                {
                    /* Project on left cut-off circle. */
                    FixedVector2 unitW = FixedCalculate.Normalize(velocity_ - leftCutOff);

                    line.direction = new FixedVector2(unitW.Y, -unitW.X);
                    line.point = leftCutOff + radius_ * invTimeHorizonObst * unitW;
                    orcaLines_.Add(line);

                    continue;
                }
                else if (t > 1 && tRight < 0)
                {
                    /* Project on right cut-off circle. */
                    FixedVector2 unitW = FixedCalculate.Normalize(velocity_ - rightCutOff);

                    line.direction = new FixedVector2(unitW.Y, -unitW.X);
                    line.point = rightCutOff + radius_ * invTimeHorizonObst * unitW;
                    orcaLines_.Add(line);

                    continue;
                }

                /*
                * Project on left leg, right leg, or cut-off line, whichever is
                * closest to velocity.
                */
                FixedInt distSqCutoff = (t < 0 || t > 1 || obstacle1 == obstacle2) ? FixedCalculate.superBigValue : FixedCalculate.Square(velocity_ - (leftCutOff + t * cutOffVector));
                FixedInt distSqLeft = tLeft < 0 ? FixedCalculate.superBigValue : FixedCalculate.Square(velocity_ - (leftCutOff + tLeft * leftLegDirection));
                FixedInt distSqRight = tRight < 0 ? FixedCalculate.superBigValue : FixedCalculate.Square(velocity_ - (rightCutOff + tRight * rightLegDirection));

                if (distSqCutoff <= distSqLeft && distSqCutoff <= distSqRight)
                {
                    /* Project on cut-off line. */
                    line.direction = -obstacle1.direction_;
                    line.point = leftCutOff + radius_ * invTimeHorizonObst * new FixedVector2(-line.direction.Y, line.direction.X);
                    orcaLines_.Add(line);

                    continue;
                }

                if (distSqLeft <= distSqRight)
                {
                    /* Project on left leg. */
                    if (isLeftLegForeign)
                    {
                        continue;
                    }

                    line.direction = leftLegDirection;
                    line.point = leftCutOff + radius_ * invTimeHorizonObst * new FixedVector2(-line.direction.Y, line.direction.X);
                    orcaLines_.Add(line);

                    continue;
                }

                /* Project on right leg. */
                if (isRightLegForeign)
                {
                    continue;
                }

                line.direction = -rightLegDirection;
                line.point = rightCutOff + radius_ * invTimeHorizonObst * new FixedVector2(-line.direction.Y, line.direction.X);
                orcaLines_.Add(line);
            }





        }


        private int linearProgram2(NativeList<Line> lines, FixedInt radius, FixedVector2 optVelocity, bool directionOpt, ref FixedVector2 result)
        {
            if (directionOpt)
            {
                /*
                * Optimize direction. Note that the optimization velocity is of
                * unit length in this case.
                */
                result = optVelocity * radius;
            }
            else if (FixedCalculate.Square(optVelocity) > FixedCalculate.Square(radius))
            {
                /* Optimize closest point and outside circle. */
                result = FixedCalculate.Normalize(optVelocity) * radius;
            }
            else
            {
                /* Optimize closest point and inside circle. */
                result = optVelocity;
            }

            for (int i = 0; i < lines.Length; ++i)
            {
                if (FixedCalculate.Det(lines[i].direction, lines[i].point - result) > 0)
                {
                    /* Result does not satisfy constraint i. Compute new optimal result. */
                    FixedVector2 tempResult = result;
                    if (!linearProgram1(lines, i, radius, optVelocity, directionOpt, ref result))
                    {
                        result = tempResult;

                        return i;
                    }
                }
            }

            return lines.Length;
        }


        private bool linearProgram1(NativeList<Line> lines, int lineNo, FixedInt radius, FixedVector2 optVelocity, bool directionOpt, ref FixedVector2 result)
        {
            FixedInt dotProduct = lines[lineNo].point * lines[lineNo].direction;
            FixedInt discriminant = FixedCalculate.Square(dotProduct) + FixedCalculate.Square(radius) - FixedCalculate.Square(lines[lineNo].point);

            if (discriminant < 0)
            {
                /* Max speed circle fully invalidates line lineNo. */
                return false;
            }

            FixedInt sqrtDiscriminant = FixedCalculate.Sqrt(discriminant);
            FixedInt tLeft = -dotProduct - sqrtDiscriminant;
            FixedInt tRight = -dotProduct + sqrtDiscriminant;

            for (int i = 0; i < lineNo; ++i)
            {
                FixedInt denominator = FixedCalculate.Det(lines[lineNo].direction, lines[i].direction);
                FixedInt numerator = FixedCalculate.Det(lines[i].direction, lines[lineNo].point - lines[i].point);

                if (FixedCalculate.fabs(denominator) <= (FixedInt)(long)1 << 5)
                {
                    /* Lines lineNo and i are (almost) parallel. */
                    if (numerator < 0)
                    {
                        return false;
                    }

                    continue;
                }

                FixedInt t = numerator / denominator;

                if (denominator >= 0)
                {
                    /* Line i bounds line lineNo on the right. */
                    tRight = FixedCalculate.Min(tRight, t);
                }
                else
                {
                    /* Line i bounds line lineNo on the left. */
                    tLeft = FixedCalculate.Max(tLeft, t);
                }

                if (tLeft > tRight)
                {
                    return false;
                }
            }

            if (directionOpt)
            {
                /* Optimize direction. */
                if (optVelocity * lines[lineNo].direction > 0)
                {
                    /* Take right extreme. */
                    result = lines[lineNo].point + tRight * lines[lineNo].direction;
                }
                else
                {
                    /* Take left extreme. */
                    result = lines[lineNo].point + tLeft * lines[lineNo].direction;
                }
            }
            else
            {
                /* Optimize closest point. */
                FixedInt t = lines[lineNo].direction * (optVelocity - lines[lineNo].point);

                if (t < tLeft)
                {
                    result = lines[lineNo].point + tLeft * lines[lineNo].direction;
                }
                else if (t > tRight)
                {
                    result = lines[lineNo].point + tRight * lines[lineNo].direction;
                }
                else
                {
                    result = lines[lineNo].point + t * lines[lineNo].direction;
                }
            }

            return true;
        }


        private void linearProgram3(NativeList<Line> lines, int numObstLines, int beginLine, FixedInt radius, ref FixedVector2 result)
        {
            FixedInt distance = 0;

            for (int i = beginLine; i < lines.Length; ++i)
            {
                if (FixedCalculate.Det(lines[i].direction, lines[i].point - result) > distance)
                {
                    /* Result does not satisfy constraint of line i. */
                    NativeList<Line> projLines = new NativeList<Line>(Allocator.Temp);
                    for (int ii = 0; ii < numObstLines; ++ii)
                    {
                        projLines.Add(lines[ii]);
                    }

                    for (int j = numObstLines; j < i; ++j)
                    {
                        Line line;

                        FixedInt determinant = FixedCalculate.Det(lines[i].direction, lines[j].direction);

                        if (FixedCalculate.fabs(determinant) <= (FixedInt)(long)1 << 5)
                        {
                            /* Line i and line j are parallel. */
                            if (lines[i].direction * lines[j].direction > 0)
                            {
                                /* Line i and line j point in the same direction. */
                                continue;
                            }
                            else
                            {
                                /* Line i and line j point in opposite direction. */
                                line.point = FixedInt.half * (lines[i].point + lines[j].point);
                            }
                        }
                        else
                        {
                            line.point = lines[i].point + (FixedCalculate.Det(lines[j].direction, lines[i].point - lines[j].point) / determinant) * lines[i].direction;
                        }
                        if (FixedCalculate.Square(lines[j].direction - lines[i].direction) > 0)
                        {
                            line.direction = FixedCalculate.Normalize(lines[j].direction - lines[i].direction);
                            projLines.Add(line);
                        }

                    }

                    FixedVector2 tempResult = result;
                    if (linearProgram2(projLines, radius, new FixedVector2(-lines[i].direction.Y, lines[i].direction.X), true, ref result) < projLines.Length)
                    {
                        /*
                        * This should in principle not happen. The result is by
                        * definition already in the feasible region of this
                        * linear program. If it fails, it is due to small
                        * FixedInting point error, and the current result is kept.
                        */
                        result = tempResult;
                    }

                    distance = FixedCalculate.Det(lines[i].direction, lines[i].point - result);
                    projLines.Dispose();
                }
            }
        }




    }



 
}
