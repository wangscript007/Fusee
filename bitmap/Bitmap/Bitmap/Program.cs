﻿using System;
using System.Drawing;

namespace Bitmape
{
    class Program
    {
        static void Main(string[] args)
        {
            int x = 25;
            int y = 25;
            string str = AppDomain.CurrentDomain.BaseDirectory;
            Bitmap img = new Bitmap(str + "Maze.bmp");
            int[,] image = new int[img.Width, img.Height];
            for (int j = 5; j < img.Height; j += y)
            {
                for (int i = 5; i < img.Width; i += x)
                {
                    Color pixel = img.GetPixel(i, j);

                    if (pixel.Equals(Color.FromArgb(255,0,0,0)))
                    {
                        image[i, j] = 1;
                    }
                    else
                    {
                        image[i, j] = 0;
                    }
                    Console.Write(image[i, j]);
                    if(x == 25) { x++; } else { x--; }
                }
                Console.WriteLine();
                if (y == 25) { y++; } else { y--; }
            }
        }
    }
}
