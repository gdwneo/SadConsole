﻿using System;
using Console = SadConsole.Consoles.Console;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SadConsole.Consoles;
using SadConsole;
using Microsoft.Xna.Framework;
using System.Runtime.Serialization;
using SadConsole.Input;

namespace SadConsole.Consoles
{
    [DataContract]
    [KnownType(typeof(LayeredConsole<LayeredConsoleMetadata>))]
    [KnownType(typeof(LayeredConsole))]
    [KnownType(typeof(LayeredConsoleMetadata))]
    [KnownType(typeof(Console))]
    public class LayeredConsole : LayeredConsole<LayeredConsoleMetadata>
    {
        public LayeredConsole(int layers, int width, int height) : base(layers, width, height) { }
    }

    [DataContract]
    public class LayeredConsoleMetadata
    {
        [DataMember]
        public string Name = "New";
        [DataMember]
        public bool IsVisible = true;
        [DataMember]
        public bool IsRemoveable = true;
        [DataMember]
        public bool IsMoveable = true;
        [DataMember]
        public bool IsRenamable = true;

        public int Index;

        public override string ToString()
        {
            return Name;
        }
    }

    [DataContract]
    public abstract class LayeredConsole<TMetadata>: Console
        where TMetadata : LayeredConsoleMetadata, new()
    {
        

        [DataMember]
        public int Width { get; protected set; }
        [DataMember]
        public int Height { get; protected set; }

        public int Layers { get { return _layers.Count; } }

        [IgnoreDataMember]
        public CellSurface ActiveLayer { get; protected set; }

        [DataMember(Name = "LayerMetadata")]
        protected List<TMetadata> _layerMetadata;

        [DataMember(Name = "Layers")]
        protected List<CellsRenderer> _layers;

        public CellsRenderer this[int index]
        {
            get { return _layers[index]; }
        }

        public LayeredConsole(int layers, int width, int height): base(width, height)
        {
            Width = width;
            Height = height;

            _layers = new List<CellsRenderer>();
            _layerMetadata = new List<TMetadata>();
            
            for (int i = 0; i < layers; i++)
                AddLayer(i.ToString());


            SetActiveLayer(0);
        }

        public virtual void SyncLayers()
        {
            if (_layers != null)
                foreach (var item in _layers)
                {
                    item.Position = this.Position;
                    item.Font = this.Font;
                }
        }

        protected override void OnFontChanged()
        {
            SyncLayers();
        }

        public void Clear(Color foreground, Color background)
        {
            // Create all layers
            for (int i = 0; i < _layers.Count; i++)
            {
                _layers[i].CellData.DefaultBackground = background;
                _layers[i].CellData.DefaultForeground = foreground;
                _layers[i].CellData.Clear();
            }
        }

        public void SetActiveLayer(int index)
        {
            if (index < 0 || index > _layers.Count - 1)
                throw new ArgumentOutOfRangeException("index");

            _cellData = _layers[index].CellData;
            ActiveLayer = _cellData;
            ResetViewArea();
        }

        public virtual void Resize(int width, int height)
        {
            Width = width;
            Height = height;

            for (int i = 0; i < _layers.Count; i++)
                _layers[i].CellData.Resize(width, height);

            ResetViewArea();
        }

        /// <summary>
        /// Moves all layers to be at the specified position.
        /// </summary>
        /// <param name="position">The position of all layers.</param>
        public void Move(Point position)
        {
            this.Position = position;

            SyncLayers();
        }

        public override void Update()
        {
            for (int i = 0; i < _layers.Count; i++)
                _layers[i].Update();
        }

        public override void Render()
        {
            for (int i = 0; i < _layers.Count; i++)
                if (_layers[i].IsVisible)
                    _layers[i].Render();
        }

        public void SetLayerMetadata(int layer, TMetadata data)
        {
            _layerMetadata[layer] = data;
        }

        public TMetadata GetLayerMetadata(int layer)
        {
            return _layerMetadata[layer];
        }

        public void RemoveLayer(int layer)
        {
            var layerObject = _layers[layer];

            _layers.RemoveAt(layer);
            _layerMetadata.RemoveAt(layer);
            SyncLayerIndex();

            if (ActiveLayer == layerObject.CellData)
                ActiveLayer = null;
        }

        public CellsRenderer AddLayer(string name)
        {
            var layer = new CellsRenderer(new CellSurface(Width, Height), Batch);
            layer.Font = this.Font;
            _layers.Add(layer);

            var metadata = CreateMetadataObject();
            metadata.Name = name;

            _layerMetadata.Add(CreateMetadataObject());

            SyncLayerIndex();
            SyncLayers();
            return layer;
        }

        public void AddLayer(CellSurface surface)
        {
            var layer = new CellsRenderer(surface, Batch);
            layer.Font = this.Font;
            _layers.Add(layer);

            _layerMetadata.Add(CreateMetadataObject());

            SyncLayerIndex();
            SyncLayers();
        }

        public CellsRenderer InsertLayer(int index)
        {
            var layer = new CellsRenderer(new CellSurface(Width, Height), Batch);
            layer.Font = this.Font;
            _layers.Insert(index, layer);

            var metadata = CreateMetadataObject();
            metadata.Name = index.ToString();

            _layerMetadata.Insert(index, metadata);

            SyncLayerIndex();
            SyncLayers();
            return layer;
        }

        public void MoveLayer(int index, int newIndex)
        {
            var layer = _layers[index];
            var layerName = _layerMetadata[index];

            _layers.RemoveAt(index);
            _layerMetadata.RemoveAt(index);

            _layers.Insert(newIndex, layer);
            _layerMetadata.Insert(newIndex, layerName);

            SyncLayerIndex();
        }

        protected virtual TMetadata CreateMetadataObject()
        {
            return new TMetadata();
        }

        public IEnumerable<CellsRenderer> GetEnumeratorForLayers()
        {
            return _layers;
        }

        [OnDeserialized]
        private void AfterDeserialized(System.Runtime.Serialization.StreamingContext context)
        {
            SyncLayerIndex();
            SetActiveLayer(0);
        }
        private CellSurface _tempSurface;
        [OnSerializing]
        private void BeforeSerializing(StreamingContext context)
        {
            _tempSurface = _cellData;
            _cellData = null;
        }

        [OnSerialized]
        private void AfterSerialized(StreamingContext context)
        {
            _cellData = _tempSurface;
            _tempSurface = null;
        }

        public void ResizeCells(int width, int height   )
        {
            for (int i = 0; i < _layers.Count; i++)
            {
                _layers[i].CellSize = new Point(width, height);
            }
        }

        private void SyncLayerIndex()
        {
            for (int i = 0; i < Layers; i++)
                _layerMetadata[i].Index = i;
        }

        /// <summary>
        /// Sets the viewarea of all layers.
        /// </summary>
        /// <param name="viewArea"></param>
        public void SetViewArea(Rectangle viewArea)
        {
            for (int i = 0; i < Layers; i++)
                _layers[i].ViewArea = viewArea;

            ViewArea = viewArea;
        }
    }

    #region Different implementation of a "layeredconsole" that I was playign with...
    //public class LayeredConsole2: IConsole
    //{
    //    private Console _fauxConsole;

    //    #region Console/Render Stuff
    //    public Console.Cursor VirtualCursor { get { return _fauxConsole.VirtualCursor; } set { _fauxConsole.VirtualCursor = value; } }

    //    public IParentConsole Parent { get { return _fauxConsole.Parent; } set { _fauxConsole.Parent = value; } }

    //    public bool CanUseKeyboard { get { return _fauxConsole.CanUseKeyboard; } set { _fauxConsole.CanUseKeyboard = value; } }

    //    public bool CanUseMouse { get { return _fauxConsole.CanUseMouse; } set { _fauxConsole.CanUseMouse = value; } }

    //    public bool CanFocus { get { return _fauxConsole.CanFocus; } set { _fauxConsole.CanFocus = value; } }

    //    public bool IsFocused { get { return _fauxConsole.IsFocused; } set { _fauxConsole.IsFocused = value; } }

    //    public bool ExclusiveFocus { get { return _fauxConsole.ExclusiveFocus; } set { _fauxConsole.ExclusiveFocus = value; } }

    //    public bool ProcessMouse(MouseInfo info)
    //    {
    //        return false;
    //    }

    //    public bool ProcessKeyboard(KeyboardInfo info)
    //    {
    //        return false;
    //    }

    //    public bool IsVisible { get { return _fauxConsole.IsVisible; } set { _fauxConsole.IsVisible = value; } }

    //    public Matrix? Transform { get { return _fauxConsole.Transform; } set { _fauxConsole.Transform = value; } }

    //    public bool UseAbsolutePositioning { get { return _fauxConsole.UseAbsolutePositioning; } set { _fauxConsole.UseAbsolutePositioning = value; } }

    //    public Microsoft.Xna.Framework.Graphics.SpriteBatch Batch { get { return _fauxConsole.Batch; } }

    //    public Point CellSize { get { return _fauxConsole.CellSize; } set { _fauxConsole.CellSize = value; } }

    //    public Point Position { get { return _fauxConsole.Position; } set { _fauxConsole.Position = value; } }

    //    public Rectangle ViewArea { get { return _fauxConsole.ViewArea; } set { _fauxConsole.ViewArea = value; } }

    //    public CellSurface CellData { get { return _fauxConsole.CellData; } set { _fauxConsole.CellData = value; } }

    //    public Font Font { get { return _fauxConsole.Font; } set { _fauxConsole.Font = value; } }

    //    public void Render()
    //    {
    //        _fauxConsole.Render();
    //    }

    //    public void Update()
    //    {

    //    }
    //    #endregion

    //}
    #endregion
}
