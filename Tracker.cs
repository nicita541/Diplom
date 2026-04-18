using System;
using System.Collections.Generic;
using System.Linq;

namespace Diplom
{
    internal class Tracker
    {
        private readonly List<Track> tracks = new List<Track>();
        private int nextTrackId = 1;

        private const int MaxMissedFrames = 5;
        private const double MaxCenterDistance = 180.0;
        private const double MinIoU = 0.05;
        private const double MinScore = 0.20;

        public void add(List<DetectedObjectInfo> detectedObjectInfo)
        {
            if (detectedObjectInfo == null)
                detectedObjectInfo = new List<DetectedObjectInfo>();

            trek(detectedObjectInfo);
        }

        private void trek(List<DetectedObjectInfo> currentFrame)
        {
            var usedTrackIds = new HashSet<int>();
            var unmatchedDetections = new List<DetectedObjectInfo>();

            foreach (var detection in currentFrame)
            {
                Track bestTrack = null;
                double bestScore = double.MinValue;

                foreach (var track in tracks)
                {
                    if (usedTrackIds.Contains(track.Id))
                        continue;

                    if (track.ClassId != detection.class_id)
                        continue;

                    var predictedBox = PredictBox(track);

                    double centerDistance = GetCenterDistance(predictedBox, detection);
                    if (centerDistance > MaxCenterDistance)
                        continue;

                    double iou = GetIoU(predictedBox, detection);
                    double sizeDifference = GetSizeDifference(predictedBox, detection);

                    if (iou < MinIoU && centerDistance > MaxCenterDistance * 0.5)
                        continue;

                    double centerScore = 1.0 / (1.0 + centerDistance / 50.0);
                    double sizeScore = Math.Max(0.0, 1.0 - sizeDifference);
                    double iouScore = iou;

                    double score = iouScore * 0.5 + centerScore * 0.35 + sizeScore * 0.15;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTrack = track;
                    }
                }

                if (bestTrack != null && bestScore >= MinScore)
                {
                    detection.track_id = bestTrack.Id;

                    bestTrack.PrevBox = CopyBox(bestTrack.LastBox);
                    bestTrack.LastBox = CopyBox(detection);
                    bestTrack.MissedFrames = 0;

                    usedTrackIds.Add(bestTrack.Id);
                }
                else
                {
                    unmatchedDetections.Add(detection);
                }
            }

            foreach (var track in tracks)
            {
                if (!usedTrackIds.Contains(track.Id))
                {
                    track.MissedFrames++;
                }
            }

            foreach (var detection in unmatchedDetections)
            {
                detection.track_id = nextTrackId;

                tracks.Add(new Track
                {
                    Id = nextTrackId,
                    ClassId = detection.class_id,
                    ClassName = detection.class_name,
                    LastBox = CopyBox(detection),
                    PrevBox = null,
                    MissedFrames = 0
                });

                nextTrackId++;
            }

            tracks.RemoveAll(t => t.MissedFrames > MaxMissedFrames);
        }

        private DetectedObjectInfo PredictBox(Track track)
        {
            if (track.LastBox == null)
                return null;

            if (track.PrevBox == null)
                return CopyBox(track.LastBox);

            double lastCx = GetCenterX(track.LastBox);
            double lastCy = GetCenterY(track.LastBox);

            double prevCx = GetCenterX(track.PrevBox);
            double prevCy = GetCenterY(track.PrevBox);

            double vx = lastCx - prevCx;
            double vy = lastCy - prevCy;

            double predictedCx = lastCx + vx * (track.MissedFrames + 1);
            double predictedCy = lastCy + vy * (track.MissedFrames + 1);

            double width = Math.Max(1, track.LastBox.x2 - track.LastBox.x1);
            double height = Math.Max(1, track.LastBox.y2 - track.LastBox.y1);

            return new DetectedObjectInfo
            {
                track_id = track.Id,
                class_id = track.ClassId,
                class_name = track.ClassName,
                confidence = track.LastBox.confidence,
                x1 = (int)Math.Round(predictedCx - width / 2.0),
                y1 = (int)Math.Round(predictedCy - height / 2.0),
                x2 = (int)Math.Round(predictedCx + width / 2.0),
                y2 = (int)Math.Round(predictedCy + height / 2.0)
            };
        }

        private double GetCenterDistance(DetectedObjectInfo a, DetectedObjectInfo b)
        {
            double dx = GetCenterX(a) - GetCenterX(b);
            double dy = GetCenterY(a) - GetCenterY(b);
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private double GetCenterX(DetectedObjectInfo box)
        {
            return (box.x1 + box.x2) / 2.0;
        }

        private double GetCenterY(DetectedObjectInfo box)
        {
            return (box.y1 + box.y2) / 2.0;
        }

        private double GetSizeDifference(DetectedObjectInfo a, DetectedObjectInfo b)
        {
            double aw = Math.Max(1, a.x2 - a.x1);
            double ah = Math.Max(1, a.y2 - a.y1);
            double bw = Math.Max(1, b.x2 - b.x1);
            double bh = Math.Max(1, b.y2 - b.y1);

            double widthDiff = Math.Abs(aw - bw) / Math.Max(aw, bw);
            double heightDiff = Math.Abs(ah - bh) / Math.Max(ah, bh);

            return (widthDiff + heightDiff) / 2.0;
        }

        private double GetIoU(DetectedObjectInfo a, DetectedObjectInfo b)
        {
            if (a == null || b == null)
                return 0;

            int interX1 = Math.Max(a.x1, b.x1);
            int interY1 = Math.Max(a.y1, b.y1);
            int interX2 = Math.Min(a.x2, b.x2);
            int interY2 = Math.Min(a.y2, b.y2);

            int interWidth = Math.Max(0, interX2 - interX1);
            int interHeight = Math.Max(0, interY2 - interY1);
            int interArea = interWidth * interHeight;

            int areaA = Math.Max(0, a.x2 - a.x1) * Math.Max(0, a.y2 - a.y1);
            int areaB = Math.Max(0, b.x2 - b.x1) * Math.Max(0, b.y2 - b.y1);

            int unionArea = areaA + areaB - interArea;
            if (unionArea <= 0)
                return 0;

            return (double)interArea / unionArea;
        }

        private DetectedObjectInfo CopyBox(DetectedObjectInfo box)
        {
            if (box == null)
                return null;

            return new DetectedObjectInfo
            {
                track_id = box.track_id,
                class_id = box.class_id,
                class_name = box.class_name,
                confidence = box.confidence,
                x1 = box.x1,
                y1 = box.y1,
                x2 = box.x2,
                y2 = box.y2
            };
        }
    }

    internal class Track
    {
        public int Id { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; }

        public DetectedObjectInfo LastBox { get; set; }
        public DetectedObjectInfo PrevBox { get; set; }

        public int MissedFrames { get; set; }
    }
}
