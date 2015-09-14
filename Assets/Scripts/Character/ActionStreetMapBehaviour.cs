﻿using System;
using ActionStreetMap.Core;
using ActionStreetMap.Core.Geometry;
using ActionStreetMap.Explorer.Infrastructure;
using ActionStreetMap.Infrastructure.Diagnostic;
using ActionStreetMap.Infrastructure.Reactive;
using ActionStreetMap.Maps.GeoCoding;
using UnityEngine;
using RenderMode = ActionStreetMap.Core.RenderMode;

namespace Assets.Scripts.Character
{
    /// <summary> Performs some initialization and listens for position changes of character.  </summary>
    public class ActionStreetMapBehaviour : MonoBehaviour
    {
        private ApplicationManager _appManager;

        private Vector3 _position = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        /// <summary>
        ///     Place name. Will be resolved to the certain GeoCoordinate via performing reverse
        ///     geocoding request to reverse geocoding server.
        /// </summary>
        public string PlaceName;

        public RenderMode RenderMode = RenderMode.Scene;

        /// <summary> Start latitude. Used if PlaceName is empty. </summary>
        public double StartLatitude = 52.5209;

        /// <summary> Start longitude. Used if PlaceName is empty. </summary>
        public double StartLongitude = 13.40793;

        /// <summary> Size of tile. </summary>
        public float TileSize = 180;

        /// <summary>
        ///     Sets start geocoordinate using desired method: direct latitude/longitude or
        ///     via reverse geocoding request for given place name.
        /// </summary>
        private void SetStartGeoCoordinate()
        {
            var coordinate = new GeoCoordinate(StartLatitude, StartLongitude);
            if (!String.IsNullOrEmpty(PlaceName))
            {
                // NOTE this will freeze UI thread as we're making web request and should wait for its result
                var place = _appManager.GetService<IGeocoder>().Search(PlaceName)
                    .Wait();

                if (place != null)
                    coordinate = place.Coordinate;
                else
                    _appManager.GetService<ITrace>()
                        .Warn("init", "Cannot resolve '{0}', will use default latitude/longitude", PlaceName);
            }
            _appManager.Coordinate = coordinate;
        }

        /// <summary> Returns config builder initialized with user defined settings. </summary>
        private ConfigBuilder GetConfigBuilder()
        {
            return ConfigBuilder.GetDefault()
                .SetTileSettings(TileSize, 40)
                .SetRenderOptions(RenderMode, new Rectangle2d(0, 0, TileSize*3, TileSize*3));
        }

        #region Unity lifecycle events

        /// <summary> Performs framework initialization once, before any Start() is called. </summary>
        void Awake()
        {
            _appManager = ApplicationManager.Instance;
            _appManager.InitializeFramework(GetConfigBuilder());

            SetStartGeoCoordinate();
        }

        /// <summary> Runs game after all Start() methods are called. </summary>
        void OnEnable()
        {
            // ASM should be started from non-UI thread
            Observable.Start(() => _appManager.RunGame(), Scheduler.ThreadPool);
        }

        /// <summary> Listens for position changes to notify framework. </summary>
        void Update()
        {
            if (RenderMode == RenderMode.Scene && _appManager.IsInitialized &&
                _position != transform.position)
            {
                _position = transform.position;
                _appManager.Move(new Vector2d(_position.x, _position.z));
            }
        }

        #endregion
    }
}