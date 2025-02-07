﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;


namespace GPXManager.entities
{
    public class JpegMetadataAdapter
    {
        private readonly string path;
        private BitmapFrame frame;
        public readonly BitmapMetadata Metadata;

        public JpegMetadataAdapter(string path)
        {
            this.path = path;
            frame = getBitmapFrame(path);
            if (frame != null)
            {
                Metadata = (BitmapMetadata)frame.Metadata.Clone();
            }
        }

        public void Save()
        {
            SaveAs(path);
        }

        public void SaveAs(string path)
        {
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(frame, frame.Thumbnail, Metadata, frame.ColorContexts));
            using (Stream stream = File.Open(path, FileMode.Create, FileAccess.ReadWrite))
            {
                encoder.Save(stream);
            }
        }

        private BitmapFrame getBitmapFrame(string path)
        {
            BitmapDecoder decoder = null;
            try
            {
                using (Stream stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    decoder = new JpegBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                }
                return decoder.Frames[0];
            }
            catch(System.IO.IOException)
            {
                return null;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return null;
            }
            
        }
    }
}
