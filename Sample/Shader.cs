using System;
using System.IO;
using System.Text;

namespace Sample
{
    /// <summary>
    /// Generates shaders at runtime based on parameters.
    /// </summary>
    public static class Shader
    {
        /// <summary>
        /// Gets the fullscreen vertex shader.
        /// </summary>
        public static String VertShader()
        {
            var str = new StringBuilder();

            str.AppendLine("#version 130");
            str.AppendLine();
            str.AppendLine("in vec3 vertexPosition;");
			str.AppendLine("out vec2 uv;");
            str.AppendLine();
            str.AppendLine("void main(void)");
            str.AppendLine("{");
            str.AppendLine("gl_Position = vec4(vertexPosition.xy * 2 - 1, 0.5, 1.0);");
            str.AppendLine("uv = (vec2(gl_Position.x, -gl_Position.y) + vec2(1.0)) / vec2(2.0);");
            str.AppendLine("}");

            WriteToFile("shader.vs", str.ToString());

            return str.ToString();
        }

        /// <summary>
        /// Gets the fragment shader.
        /// </summary>
        public static String FragShader(Polynomial poly, ShadingType type, AAQuality aa)
        {
            var shader = String.Join("\n", new[]
            {
                "#version 130\n",
                FragArithmetic(),
                FragPolyRoots(poly, "poly"),
                FragPolyRoots(Polynomial.Derivative(poly), "derv"),
                FragIterate(),
                FragColorize(type),
                FragShade(),
				FragMainSampler(aa)
            });

            WriteToFile("shader.fs", shader);

            return shader;
        }

        private static void WriteToFile(String path, String shader)
        {
            var stream = new StreamWriter(File.OpenWrite(path));
            stream.Write(shader);
            stream.Close();
        }

        private static String FragArithmetic()
        {
            var str = new StringBuilder();

            // Squared magnitude
            str.AppendLine("float csqrabs(vec2 p)");
            str.AppendLine("{");
            str.AppendLine("    return dot(p, p);");
            str.AppendLine("}");
            str.AppendLine();

            // Magnitude
            str.AppendLine("float cabs(vec2 p)");
            str.AppendLine("{");
            str.AppendLine("    return sqrt(csqrabs(p));");
            str.AppendLine("}");
            str.AppendLine();

            // Multiplication (addition is built-in)
            str.AppendLine("vec2 cmul(vec2 p, vec2 q)");
            str.AppendLine("{");
            str.AppendLine("    return vec2(p.x * q.x - p.y * q.y, p.y * q.x + p.x * q.y);");
            str.AppendLine("}");
            str.AppendLine();

            // Division
            str.AppendLine("vec2 cdiv(vec2 p, vec2 q)");
            str.AppendLine("{");
            str.AppendLine("    return vec2(p.x * q.x + p.y * q.y, p.y * q.x - p.x * q.y) / csqrabs(q);");
            str.AppendLine("}");

            return str.ToString();
        }

        private static String FragPolyRoots(Polynomial poly, String name)
        {
            var roots = Polynomial.Roots(poly);
            var str = new StringBuilder();

            str.AppendLine(String.Format("vec2 {0}(vec2 z)", name));
            str.AppendLine("{");
            str.AppendLine(String.Format("    vec2 r = vec2({0}, {1});", roots.Item2.Real, roots.Item2.Imaginary));

            foreach (var root in roots.Item1)
                str.AppendLine(String.Format("    r = cmul(r, z - vec2({0}, {1}));", root.Real, root.Imaginary));

            str.AppendLine("    return r;");
            str.AppendLine("}");

            return str.ToString();
        }

        private static String FragIterate()
        {
            var str = new StringBuilder();

            str.AppendLine("uniform vec2 aCoeff;");
            str.AppendLine("uniform vec2 kCoeff;");
            str.AppendLine("uniform uint iters;");
            str.AppendLine();
            str.AppendLine("vec2 iterate(vec2 z, out float speed, out uint t)");
            str.AppendLine("{");
            str.AppendLine("    speed = 0;");
            str.AppendLine();
            str.AppendLine("    for (t = uint(0); t < iters; ++t)");
            str.AppendLine("    {");
            str.AppendLine("        vec2 r = z;");
            str.AppendLine("        z -= cmul(cdiv(poly(z), derv(z)), aCoeff) + kCoeff;");
            str.AppendLine("        float l = csqrabs(r - z);");
            str.AppendLine("        speed += exp(-inversesqrt(l));");
            str.AppendLine("        if (l < 1e-8) break;");
            str.AppendLine("    }");
            str.AppendLine();
            str.AppendLine("    return z;");
            str.AppendLine("}");

            return str.ToString();
        }

        private static String FragColorize(ShadingType type)
        {
            var str = new StringBuilder();

            str.AppendLine("uniform vec4 palette;");
            str.AppendLine();
            str.AppendLine("vec3 colorize(vec2 z, vec2 r, float speed, float t)");
            str.AppendLine("{");

            switch (type)
            {
                case ShadingType.Standard:
                    str.AppendLine("    if (isnan(speed)) return vec3(1);");
                    str.AppendLine("    speed *= speed * 0.05;");
                    str.AppendLine("    vec3 retval = (sin(vec3(r.x) * palette.xyz) + sin(vec3(r.y) * palette.xyz) + 2) * speed;");
                    str.AppendLine("    return retval / (retval + vec3(1));");
                    break;
                case ShadingType.Negative:
                    str.AppendLine("    if (speed == 0) return vec3(1);");
                    str.AppendLine("    if (isnan(speed)) return vec3(0);");
                    str.AppendLine("    vec3 retval = (sin(vec3(r.x) * palette.xyz) + sin(vec3(r.y) * palette.xyz) + 2) / speed;");
                    str.AppendLine("    return retval / (retval + vec3(1));");
                    break;
				case ShadingType.Flat:
					str.AppendLine("    return (sin(vec3(r.x) * palette.xyz) + sin(vec3(r.y) * palette.xyz) + 2) * t * 2;");
					break;
                default:
                    str.AppendLine("    return vec3(0.87, 0, 1);");
                    break;
            }

            str.AppendLine("}");

            return str.ToString();
        }

        private static String FragShade()
        {
            var str = new StringBuilder();

            str.AppendLine("uniform float intensity;");
            str.AppendLine();
            str.AppendLine("vec3 shade(vec2 z)");
            str.AppendLine("{");
            str.AppendLine("    uint t;");
            str.AppendLine("    float speed;");
            str.AppendLine("    vec2 r = iterate(z, speed, t);");
            str.AppendLine("    return colorize(z, r, pow(speed, intensity), t * intensity / iters);");
            str.AppendLine("}");

            return str.ToString();
        }

		private static String FragMainSampler(AAQuality aa)
        {
            var str = new StringBuilder();

            str.AppendLine("uniform vec2 offset;");
            str.AppendLine("uniform float zoom;");
            str.AppendLine("uniform vec2 dims;");
			str.AppendLine("in vec2 uv;");
            str.AppendLine();
            str.AppendLine("vec3 plot_fractal(vec2 z)");
            str.AppendLine("{");

			int samples = (int)aa;

            if (samples == 1)
                str.AppendLine("    return shade(z);");
            else
            {
                str.AppendLine("    vec2 d = vec2(zoom) / dims;");
                str.AppendLine("    vec3 shading = vec3(0);");
                str.AppendLine();

                for (int y = 0; y < samples; ++y)
                    for (int x = 0; x < samples; ++x)
                    {
                        double px = (x / (double)(samples - 1) - 0.5) / 2;
                        double py = (y / (double)(samples - 1) - 0.5) / 2;

                        str.AppendLine(String.Format("    shading += shade(z + vec2({0}, {1}) * d);", px, py));
                    }

                str.AppendLine();
                str.AppendLine(String.Format("    return shading / {0};", samples * samples));
            }

            str.AppendLine("}");
            str.AppendLine();
            str.AppendLine("void main(void)");
            str.AppendLine("{");
            str.AppendLine("    float ratio = dims.x / dims.y; /* Calculates aspect ratio */");
            str.AppendLine("    vec2 z = (uv + vec2(-0.5)) * vec2(ratio, 1) * zoom + offset;");
            str.AppendLine("    gl_FragColor = vec4(plot_fractal(z), 0); /* Draws fractal */");
            str.AppendLine("}");

            return str.ToString();
        }
    }
}
