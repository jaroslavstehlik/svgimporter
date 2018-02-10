// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter.Data
{
    using Utils;

    public class QuadTreeNode<T> : System.Object
    {
        public T data;
        public SVGBounds bounds;
        public QuadTreeCell<T> cell;

        public QuadTree<T> quadTree
        {
            get
            {
                return cell.quadTree;
            }
        }

        protected internal int _depth;

        public int depth
        {
            get
            {
                return _depth;
            }
        }

        public QuadTreeNode(T data, SVGBounds bounds)
        {
            this.data = data;
            this.bounds = bounds;
        }

        public QuadTreeNode(T data, SVGBounds bounds, QuadTreeCell<T> cell)
        {
            this.data = data;
            this.bounds = bounds;
            this.cell = cell;
        }

        public void Move(SVGBounds bounds)
        {
            if (this.bounds.Compare(bounds))
                return;

            this.bounds.ApplyBounds(bounds);

            if (this.cell.bounds.Contains(bounds))
                return;

            QuadTreeCell<T> root = this.cell.root;
            Remove();
            root.Add(this);
        }

        public void Remove()
        {
            cell.nodes.Remove(this);
            if (cell.nodes.Count == 0)
                cell.CleanUnusedCells();
        }
    }

    public class QuadTreeCell<T> : System.Object
    {

        const int DEFAULT_MAX_CAPACITY = 1;
        public int maxCapacity = 1;
        public SVGBounds bounds;
        public QuadTreeCell<T> parent;
        public QuadTreeCell<T> topLeft;
        public QuadTreeCell<T> topRight;
        public QuadTreeCell<T> bottomLeft;
        public QuadTreeCell<T> bottomRight;
        public List<QuadTreeNode<T>> nodes;
        public QuadTree<T> quadTree;
        protected internal int _depth;

        public int depth
        {
            get
            {
                return _depth;
            }
        }

        public QuadTreeCell<T> root
        {
            get
            {
                return quadTree._root;
            }
        }

        internal QuadTreeCell<T> FindRoot(QuadTreeCell<T> current)
        {
            if (current.parent != null)
            {
                return FindRoot(current.parent);
            }

            return current;
        }

        public QuadTreeCell(SVGBounds bounds)
        {
            this.bounds = bounds;
            this.parent = null;
            this.quadTree = null;
            this.maxCapacity = DEFAULT_MAX_CAPACITY;
        }

        public QuadTreeCell(SVGBounds bounds, int maxCapacity)
        {
            this.bounds = bounds;
            this.parent = null;
            this.quadTree = null;
            this.maxCapacity = maxCapacity;
        }

        public QuadTreeCell(SVGBounds bounds, QuadTreeCell<T> parent, int maxCapacity)
        {
            this.bounds = bounds;
            this.parent = parent;
            this.quadTree = null;
            this.maxCapacity = maxCapacity;
        }

        public QuadTreeCell(SVGBounds bounds, QuadTreeCell<T> parent, QuadTree<T> quadTree, int maxCapacity)
        {
            this.bounds = bounds;
            this.parent = parent;
            this.quadTree = quadTree;
            this.maxCapacity = maxCapacity;
        }

        public QuadTreeNode<T> Add(T data, SVGBounds bounds)
        {
            return Add(new QuadTreeNode<T>(data, bounds));
        }

        public QuadTreeNode<T> Add(QuadTreeNode<T> node)
        {
            if (nodes == null)
                nodes = new List<QuadTreeNode<T>>();

            //Debug.Log("Add");
            //Debug.Log(this.bounds);
            //Debug.Log(bounds);

            if (this.bounds.Contains(node.bounds))
            {
                // It is inside our region
                bool leftSide = node.bounds.maxX <= this.bounds.center.x;
                //bool rightSide = node.bounds.minX >= this.bounds.center.x;
                bool topSide = node.bounds.minY >= this.bounds.center.y;
                //bool bottomSide = node.bounds.maxY <= this.bounds.center.y;

                bool intersectsLeft = node.bounds.minX < this.bounds.center.x;
                bool intersectsRight = node.bounds.maxX > this.bounds.center.x;
                bool intersectsTop = node.bounds.maxY > this.bounds.center.y;
                bool intersectsBottom = node.bounds.minY < this.bounds.center.y;
                /*
                        Debug.Log("Iside Region: "+data.GetHashCode()+", leftSide: "+leftSide+", rightSide: "+rightSide+", topSide: "+topSide+", bottomSide: "+bottomSide
                                  +"\n node.bounds.maxX: "+node.bounds.maxX+" <= this.bounds.center.x: "+this.bounds.center.x
                                  +"\n node.bounds.minX: "+node.bounds.minX+" >= this.bounds.center.x: "+this.bounds.center.x
                                  +"\n node.bounds.minY: "+node.bounds.minY+" >= this.bounds.center.y: "+this.bounds.center.y
                                  +"\n node.bounds.maxY: "+node.bounds.maxY+" <= this.bounds.center.y: "+this.bounds.center.y

                                  );
                        */
                if ((intersectsLeft && intersectsRight) || (intersectsTop && intersectsBottom))
                {
                    //Debug.Log("Overlays more than one region: "+data.GetHashCode());
                    node.cell = this;
                    node._depth = this._depth;
                    nodes.Add(node);
                } else
                {
                    if (nodes.Count < maxCapacity)
                    {
                        node.cell = this;
                        node._depth = this._depth;
                        nodes.Add(node);
                    } else
                    {
                        //Debug.Log("Max Capacity Reached: "+data.GetHashCode());
                        if (topSide)
                        {
                            if (leftSide)
                            {
                                if (topLeft == null)
                                    topLeft = new QuadTreeCell<T>(new SVGBounds(this.bounds.minX, this.bounds.center.y, this.bounds.center.x, this.bounds.maxY), this, this.quadTree, maxCapacity);

                                topLeft._depth = _depth + 1;
                                topLeft.Add(node);

                            } else
                            {
                                if (topRight == null)
                                    topRight = new QuadTreeCell<T>(new SVGBounds(this.bounds.center.x, this.bounds.center.y, this.bounds.maxX, this.bounds.maxY), this, this.quadTree, maxCapacity);

                                topRight._depth = _depth + 1;
                                topRight.Add(node);
                            }
                        } else
                        {
                            if (leftSide)
                            {
                                if (bottomLeft == null)
                                    bottomLeft = new QuadTreeCell<T>(new SVGBounds(this.bounds.minX, this.bounds.minY, this.bounds.center.x, this.bounds.center.y), this, this.quadTree, maxCapacity);

                                bottomLeft._depth = _depth + 1;
                                bottomLeft.Add(node);
                                        
                            } else
                            {
                                if (bottomRight == null)
                                    bottomRight = new QuadTreeCell<T>(new SVGBounds(this.bounds.center.x, this.bounds.minY, this.bounds.maxX, this.bounds.center.y), this, this.quadTree, maxCapacity);

                                bottomRight._depth = _depth + 1;
                                bottomRight.Add(node);
                            }
                        }
                    }
                }
            } else
            {
                node.cell = this;
                node._depth = this._depth;
                nodes.Add(node);
                //return node;
                /*
                // TODO Expanding quadTree

                // It is outside our region
                bool outsideLeft = node.bounds.minX < this.bounds.minX;
                bool outsideRight = node.bounds.maxX > this.bounds.maxX;
                bool outsideTop = node.bounds.maxY > this.bounds.maxY;
                bool outsideBottom = node.bounds.minY < this.bounds.minY;

                //bool left = node.bounds.center.x < this.bounds.center.x;
                //bool right = node.bounds.center.x >= this.bounds.center.x;
                //bool top = node.bounds.center.y >= this.bounds.center.y;
                //bool bottom = node.bounds.center.y < this.bounds.center.y;
                //Debug.Log("Out of region: "+data.GetHashCode()+", outsideLeft: "+outsideLeft+", outsideRight: "+outsideRight+", outsideTop: "+outsideTop+", outsideBottom: "+outsideBottom);

                if (outsideTop)
                {
                    if (outsideLeft)
                    {
                        parent = new QuadTreeCell<T>(new SVGBounds(this.bounds.minX - this.bounds.size.x, this.bounds.minY, this.bounds.maxX, this.bounds.maxY + this.bounds.size.y), null, this.quadTree, maxCapacity);
                        if (!isCellEmpty)
                        {
                            parent.bottomRight = this;
                        }
                        quadTree._root = parent;
                        parent._depth = _depth - 1;
                        parent.Add(node);
                    } 

                    if (outsideRight)
                    {
                        parent = new QuadTreeCell<T>(new SVGBounds(this.bounds.minX, this.bounds.minY, this.bounds.maxX + this.bounds.size.x, this.bounds.maxY + this.bounds.size.y), null, this.quadTree, maxCapacity);
                        if (!isCellEmpty)
                            parent.bottomLeft = this;
                        quadTree._root = parent;
                        parent._depth = _depth - 1;
                        parent.Add(node);
                    }
                } 
                if (outsideBottom)
                {
                    if (outsideLeft)
                    {
                        parent = new QuadTreeCell<T>(new SVGBounds(this.bounds.minX - this.bounds.size.x, this.bounds.minY - this.bounds.size.y, this.bounds.maxX, this.bounds.maxY), null, this.quadTree, maxCapacity);
                        if (!isCellEmpty)
                            parent.topRight = this;
                        quadTree._root = parent;
                        parent._depth = _depth - 1;
                        parent.Add(node);
                    }

                    if (outsideRight)
                    {
                        parent = new QuadTreeCell<T>(new SVGBounds(this.bounds.minX, this.bounds.minY - this.bounds.size.y, this.bounds.maxX + this.bounds.size.x, this.bounds.maxY), null, this.quadTree, maxCapacity);
                        if (!isCellEmpty)    
                            parent.topLeft = this;
                        quadTree._root = parent;
                        parent._depth = _depth - 1;
                        parent.Add(node);
                    }
                }
                */
            }

            return node;
        }

        public List<QuadTreeNode<T>> Contains(Vector2 point)
        {
            if (!this.bounds.Contains(point))
                return null;

            List<QuadTreeNode<T>> output = null;

            if (nodes != null && nodes.Count > 0)
            {
                output = new List<QuadTreeNode<T>>();
                        
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes [i].bounds.Contains(point))
                        output.Add(nodes [i]);
                }
                        
                if (output.Count == 0)
                    output = null;
            }

            if (topLeft != null)
            {
                List<QuadTreeNode<T>> cellOutput = topLeft.Contains(point);
                if (cellOutput != null)
                {
                    if (output == null)
                        output = new List<QuadTreeNode<T>>();
                    output.AddRange(cellOutput);
                }
            }

            if (topRight != null)
            {
                List<QuadTreeNode<T>> cellOutput = topRight.Contains(point);
                if (cellOutput != null)
                {
                    if (output == null)
                        output = new List<QuadTreeNode<T>>();
                    output.AddRange(cellOutput);
                }
            }

            if (bottomLeft != null)
            {
                List<QuadTreeNode<T>> cellOutput = bottomLeft.Contains(point);
                if (cellOutput != null)
                {
                    if (output == null)
                        output = new List<QuadTreeNode<T>>();
                    output.AddRange(cellOutput);
                }
            }

            if (bottomRight != null)
            {
                List<QuadTreeNode<T>> cellOutput = bottomRight.Contains(point);
                if (cellOutput != null)
                {
                    if (output == null)
                        output = new List<QuadTreeNode<T>>();
                    output.AddRange(cellOutput);
                }
            }

            return output;
        }

        public List<QuadTreeNode<T>> Contains(SVGBounds bounds)
        {
            if (!this.bounds.Intersects(bounds))
                return null;
                    
            List<QuadTreeNode<T>> output = null;
                    
            if (nodes != null && nodes.Count > 0)
            {
                output = new List<QuadTreeNode<T>>();
                        
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (bounds.Contains(nodes [i].bounds))
                        output.Add(nodes [i]);
                }
                        
                if (output.Count == 0)
                    output = null;
            }
                    
            if (topLeft != null)
            {
                List<QuadTreeNode<T>> cellOutput = topLeft.Contains(bounds);
                if (cellOutput != null)
                {
                    if (output == null)
                        output = new List<QuadTreeNode<T>>();
                    output.AddRange(cellOutput);
                }
            }
                    
            if (topRight != null)
            {
                List<QuadTreeNode<T>> cellOutput = topRight.Contains(bounds);
                if (cellOutput != null)
                {
                    if (output == null)
                        output = new List<QuadTreeNode<T>>();
                    output.AddRange(cellOutput);
                }
            }
                    
            if (bottomLeft != null)
            {
                List<QuadTreeNode<T>> cellOutput = bottomLeft.Contains(bounds);
                if (cellOutput != null)
                {
                    if (output == null)
                        output = new List<QuadTreeNode<T>>();
                    output.AddRange(cellOutput);
                }
            }
                    
            if (bottomRight != null)
            {
                List<QuadTreeNode<T>> cellOutput = bottomRight.Contains(bounds);
                if (cellOutput != null)
                {
                    if (output == null)
                        output = new List<QuadTreeNode<T>>();
                    output.AddRange(cellOutput);
                }
            }
                    
            return output;
        }

        public List<QuadTreeNode<T>> Intersects(SVGBounds bounds)
        {
            if (!this.bounds.Intersects(bounds))
                return null;
                    
            List<QuadTreeNode<T>> output = null;
                    
            if (nodes != null && nodes.Count > 0)
            {
                output = new List<QuadTreeNode<T>>();

                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes [i].bounds.Intersects(bounds))
                        output.Add(nodes [i]);
                }

                if (output.Count == 0)
                    output = null;
            }
                    
            if (topLeft != null)
            {
                List<QuadTreeNode<T>> cellOutput = topLeft.Intersects(bounds);
                if (cellOutput != null)
                {
                    if (output == null)
                        output = new List<QuadTreeNode<T>>();
                    output.AddRange(cellOutput);
                }
            }
                    
            if (topRight != null)
            {
                List<QuadTreeNode<T>> cellOutput = topRight.Intersects(bounds);
                if (cellOutput != null)
                {
                    if (output == null)
                        output = new List<QuadTreeNode<T>>();
                    output.AddRange(cellOutput);
                }
            }
                    
            if (bottomLeft != null)
            {
                List<QuadTreeNode<T>> cellOutput = bottomLeft.Intersects(bounds);
                if (cellOutput != null)
                {
                    if (output == null)
                        output = new List<QuadTreeNode<T>>();
                    output.AddRange(cellOutput);
                }
            }
                    
            if (bottomRight != null)
            {
                List<QuadTreeNode<T>> cellOutput = bottomRight.Intersects(bounds);
                if (cellOutput != null)
                {
                    if (output == null)
                        output = new List<QuadTreeNode<T>>();
                    output.AddRange(cellOutput);
                }
            }
                    
            return output;
        }   

        // todo
        public List<QuadTreeNode<T>> NearestNeighbour(Vector2 point)
        {
            return null;
        }

        public void Clear()
        {
            if (nodes != null)
            {
                nodes.Clear();
                nodes = null;
            }
                            
            if (topLeft != null)
            {
                topLeft.Clear();
                topLeft = null;
            }

            if (topRight != null)
            {
                topRight.Clear();
                topRight = null;
            }

            if (bottomLeft != null)
            {
                bottomLeft.Clear();
                bottomLeft = null;
            }

            if (bottomRight != null)
            {
                bottomRight.Clear();
                bottomRight = null;
            }

            _depth = 0;
        }

        public void Remove()
        {
            if (parent != null)
            {
                if (parent.topLeft != null && parent.topLeft == this)
                {
                    parent.topLeft.Clear();
                    parent.topLeft = null;
                } else if (parent.topRight != null && parent.topRight == this)
                {
                    parent.topRight.Clear();
                    parent.topRight = null;
                } else if (parent.bottomLeft != null && parent.bottomLeft == this)
                {
                    parent.bottomLeft.Clear();
                    parent.bottomLeft = null;
                } else if (parent.bottomRight != null && parent.bottomRight == this)
                {
                    parent.bottomRight.Clear();
                    parent.bottomRight = null;
                }
            }
        }
                
        public bool isCellEmpty
        {
            get
            {
                return (nodes == null || nodes.Count == 0) && topLeft == null && topRight == null && bottomLeft == null && bottomRight == null;
            }
        }

        public void CleanUnusedCells()
        {
            CleanUnusedCells(this);
        }

        public static void CleanUnusedCells(QuadTreeCell<T> cell)
        {
            if (cell == null || !cell.isCellEmpty)
                return;

            cell.Remove();
            CleanUnusedCells(cell.parent);
        }
    }

    public class QuadTree<T> : System.Object
    {
        protected internal QuadTreeCell<T> _root;
        protected internal SVGBounds _originalBounds;
        protected internal int _originalMaxCapacity = 1;

        public QuadTree(SVGBounds bounds)
        {
            _originalBounds = bounds;
            _root = new QuadTreeCell<T>(bounds, null, this, _originalMaxCapacity);
            _root._depth = 0;
        }
                
        public QuadTree(SVGBounds bounds, int maxCapacity)
        {
            _originalBounds = bounds;
            _originalMaxCapacity = maxCapacity;
            _root = new QuadTreeCell<T>(bounds, null, this, _originalMaxCapacity);
            _root._depth = 0;
        }

        public QuadTreeCell<T> root
        {
            get
            {
                return _root;
            }
        }

        public QuadTreeNode<T> Add(T data, SVGBounds bounds)
        {
            return _root.Add(data, bounds);
        }

        public List<QuadTreeNode<T>> Contains(Vector2 point)
        {
            return _root.Contains(point);
        }

        public List<QuadTreeNode<T>> Contains(SVGBounds bounds)
        {
            return _root.Contains(bounds);
        }

        public List<QuadTreeNode<T>> Intersects(SVGBounds bounds)
        {
            return _root.Intersects(bounds);
        }

        public void Clear()
        {
            _root.Clear();
        }

        public void Reset()
        {
            _root.Clear();
            _root = new QuadTreeCell<T>(_originalBounds, null, this, _originalMaxCapacity);
            _root._depth = 0;
        }
    }
}
