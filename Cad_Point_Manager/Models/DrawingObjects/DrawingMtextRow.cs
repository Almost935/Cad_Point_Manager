using Cad_Point_Manager.Common;
using DocumentFormat.OpenXml.Drawing;
using SharpDX;
using System.Diagnostics;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingMtextRow : IDisposable
    {
        #region Fields
        private List<DrawingMtextSegment> _segments = [];
        #endregion

        #region Properties
        public float Height { get; set; } = 0;
        public float MaxWidth { get; set; }
        public Enums.TextAlignment TextAlignment { get; set; }
        public Vector3 BaseRowPosition { get; set; } = Vector3.Zero;

        public List<DrawingMtextSegment> Segments => _segments;
        public float Width => (float)Segments.Sum(s => s.Bounds.Width) + (Segments.Count > 1 ? Segments.Skip(1).Sum(s => s.SpaceWidth) : 0f);

        public Vector3 CurrentTranslate { get; private set; } = Vector3.Zero;
        public Vector3 OverallTranslate { get; private set; } = Vector3.Zero;
        #endregion

        #region Constructors
        public DrawingMtextRow(List<DrawingMtextSegment> segments, Vector3 basePosition, float maxWidth)
        {
            _segments = segments;
            TextAlignment = _segments.First().TextAlignment;
            BaseRowPosition = basePosition;
            MaxWidth = maxWidth;
        }
        #endregion

        #region Methods
        public void UpdateBounds()
        {
            foreach (var segment in Segments)
            {
                segment.UpdateBounds();
            }
        }
        public void AddSegment(DrawingMtextSegment segment)
        {
            Segments.Add(segment);
            float rowHeight = segment.TextHeight;

            if (rowHeight > Height) { Height = rowHeight; }
        }
        public void ClearSegments()
        {
            Segments.ForEach(s => s.Dispose());
            Segments.Clear();
            Height = 0;
        }

        public void GetHeight()
        {
            for (int i = 0; i < Segments.Count; i++)
            {
                var segment = Segments[i];
                float rowHeight = segment.TextHeight;

                if (rowHeight > Height) { Height = rowHeight; }
            }
        }
        public void SetTextSegmentsXOffset()
        {
            DrawingMtextSegment prevSegment = null;
            float currentXOffset = 0;
            for (int i = 0; i < Segments.Count; i++)
            {
                var segment = Segments[i];

                //if (prevSegment is not null) 
                //{
                //    if (prevSegment.TextRenderStyle == Text)
                //}

                var prevAdvanceWidth = prevSegment?.AdvanceWidth ?? 0;
                currentXOffset += prevAdvanceWidth;

                segment.XOffset = currentXOffset;
                prevSegment = segment;
            }
        }

        public void UpdateTranslate(Vector3 offset)
        {
            CurrentTranslate += offset;
            OverallTranslate += offset;
        }
        public void ApplyTranslate()
        {
            foreach (var segment in Segments)
            {
                segment.ApplyTranslate(CurrentTranslate);
            }
            CurrentTranslate = Vector3.Zero; // Reset current translate after applying to segments
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Segments.ForEach(s => s.Dispose());
                    Segments.Clear();
                }
                disposedValue = true;
            }
        }

        // Override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~DrawingMtextRow()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // Uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
