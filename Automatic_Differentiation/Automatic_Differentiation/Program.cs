using System;
using System.Collections.Generic;

namespace Automatic_Differentiation
{
    class Program
    {
        static void Main(string[] args)
        {
            // y = a exp(x) * (exp(x) + b)
            // x = 1, a = 2, b = 3 -> y = 31.0878, dy/dx = 45.8659

            AutoDiff.AutoDiff x = 1;
            var a = 2;
            var b = 3;

            // y = (a * exp(x)) * (exp(x) + b)
            var c = AutoDiff.AutoDiffMath.Exp(x);
            var d = a * c;
            var e = c + b;
            var y = d * e;
            y.GetDifferential();

            // yとdy/dxを出力
            Console.WriteLine(y.Val + "\t" + x.Dif);

            // 中間変数に分けなくてもいける
            //y = a * AutoDiff.AutoDiffMath.Exp(x) * (AutoDiff.AutoDiffMath.Exp(x) + b);
            //y.GetDifferential();

            // yとdy/dxを出力
            //Console.WriteLine(y.Val + "\t" + x.Dif);

            // y = 5x^2 をループで書く
            //x = 3;
            //y = 0;

            //for (int i = 0; i < 5; i++)
            //{
            //    y += x * x;
            //}

            //y.GetDifferential();

            // yとdy/dxを出力
            //Console.WriteLine(y.Val + "\t" + x.Dif);

            // y = (x - 5)^2 の極小値を最急降下法で求める
            //x = 20; // 適当な初期値

            //y = (x - 5) * (x - 5);
            //y.GetDifferential();

            //while (x.Dif * x.Dif >= 1e-20)
            //{
            //    x = x.Val - 0.1 * x.Dif; // xの更新

            //    y = (x - 5) * (x - 5);
            //    y.GetDifferential();
            //}

            // yの極小値とその時のx
            //Console.WriteLine(y.Val + "\t" + x.Val);
        }
    }
}