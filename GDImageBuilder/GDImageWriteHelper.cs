using GDImageBuilder.DiscUtils;
using GDImageBuilder.DiscUtils.Iso9660;
using GDImageBuilder.DiscUtils.Raw;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GDImageBuilder
{
    internal class GDImageWriteHelper
    {
        public static long WriteBootSector(bool isRawMode, BuiltStream isoStream, FileStream destStream, byte[] buffer, ref int currentLBA, ref long currentBytes, byte[] bootstrapData)
        {
            if (isRawMode)
            {
                byte[] resultSector;
                for (int i = 0; i < bootstrapData.Length; i += buffer.Length)
                {
                    Array.Copy(bootstrapData, i, buffer, 0, buffer.Length);
                    resultSector = SectorConversion.ConvertSectorToRawMode1(buffer, currentLBA++);
                    destStream.Write(resultSector, 0, resultSector.Length);
                    currentBytes += buffer.Length;
                }
            }
            else
            {
                destStream.Write(bootstrapData, 0, bootstrapData.Length);
            }

            isoStream.Seek(bootstrapData.Length, SeekOrigin.Begin);
            return (long)bootstrapData.Length; // bytesWritten
        }

        public static void WriteSector(bool isRawMode, BuiltStream isoStream, FileStream destStream, byte[] buffer, ref int currentLBA, ref int numRead)
        {
            if (isRawMode)
            {
                byte[] resultSector;
                while (numRead != 0 && numRead < buffer.Length)
                {
                    // We need all 2048 bytes for a complete sector!
                    int localRead = isoStream.Read(buffer, numRead, buffer.Length - numRead);
                    numRead += localRead;
                    if (localRead == 0)
                    {
                        for (int i = numRead; i < buffer.Length; i++)
                        {
                            buffer[i] = 0;
                        }
                        break; // Prevent infinite loop
                    }
                }
                resultSector = SectorConversion.ConvertSectorToRawMode1(buffer, currentLBA++);
                destStream.Write(resultSector, 0, resultSector.Length);
            }
            else
            {
                destStream.Write(buffer, 0, numRead);
            }
        }        
    }
}
