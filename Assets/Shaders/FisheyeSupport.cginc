float CalculateD(float2 xy, float4 distortion)
{
	float k1 = distortion[0];
	float k2 = distortion[1];
	float k3 = distortion[2];
	float k4 = distortion[3];

	float r = sqrt(xy.x * xy.x + xy.y * xy.y);
	float theta = atan(r);
	float theta2 = theta * theta;
	float theta4 = theta2 * theta2;
	float theta6 = theta2 * theta4;
	float theta8 = theta4 * theta4;
	float theta_d = theta * (1 + k1 * theta2 + k2 * theta4 + k3 * theta6 + k4 * theta8);
	float D = 1;
	if (r > .0001)
	{
		D = theta / r;
	}

	return D;
}

float F1(float2 xy, float2 ab, float4 distortion)
{
	float D = CalculateD(xy, distortion);
	return xy.x * D - ab.x;
}

float F2(float2 xy, float2 ab, float4 distortion)
{
	float D = CalculateD(xy, distortion);
	return xy.y * D - ab.y;
}



float2x2 Inverse2x2(float2x2 J)
{
	float det = determinant(J);
	return (1 / det) * float2x2(J._m11, -J._m01, -J._m10, J._m00);
}

float2x2 Jacobian2x2(float2 xy, float2 ab, float delta, float4 distortion)
{
	float f1 = F1(xy, ab.xy, distortion);
	float f2 = F2(xy, ab.xy, distortion);
	float2 dx = float2(xy.x + delta, xy.y);
	float2 dy = float2(xy.x, xy.y + delta);
	float df1dx = (F1(dx, ab.xy, distortion) - f1) / delta;
	float df1dy = (F1(dy, ab.xy, distortion) - f1) / delta;
	float df2dx = (F2(dx, ab.xy, distortion) - f2) / delta;
	float df2dy = (F2(dy, ab.xy, distortion) - f2) / delta;

	return float2x2(df1dx, df1dy, df2dx, df2dy);
}

float2 ComputeUV(float2 uv, float4 distortion, float2 fovs)
{
	// uv calculation
	float xlim = -tan(radians(fovs.x * 0.5));
	float h = sqrt(xlim * xlim + 1);
	float ylim = h * tan(radians(fovs.y * 0.5));
	
	float a = (uv.x * 2 * xlim) - xlim;
	float b = (uv.y * 2 * ylim) - ylim;
	float2 ab = float2(a, b);
	float2 xy = ab;

	float delta = 0.01;

	for (int i = 0; i < 100; i++)
	{
		float f1 = F1(xy, ab.xy, distortion);
		float f2 = F2(xy, ab.xy, distortion);
		float2x2 J = Jacobian2x2(xy, ab, delta, distortion);
		float2x2 Ji = Inverse2x2(J);

		float2 xyNew = xy - mul(Ji, float2(f1, f2));
		float2 diff = xyNew - xy;

		if (length(diff) < 0.001)
		{
			xy = xyNew;
			break;
		}
		xy = xyNew;
	}

	float u = (xlim + xy.x) / (2 * xlim);
	float v = (ylim + xy.y) / (2 * ylim);
	uv = float2(u, v);
	
	return uv;
}

float2 ComputeUV2(float2 uv, float4 distortion, float2 fovs)
{
	// calculate inner boundary and ratio (inner/outer)
	float outerx = -tan(radians(fovs.x * 0.5));
	float h = sqrt(outerx * outerx + 1);
	float outery = h * tan(radians(fovs.y * 0.5));
	float2 outer = float2(outerx, outery);
	float D = CalculateD(outer, distortion);
	float2 inner = outer * D;
	float ratio = inner.x / outer.x;
	//float ratio = 1;

	// ab calculation
	float a = (uv.x * 2 * outerx) - outerx;
	float b = (uv.y * 2 * outery) - outery;
	float2 ab = float2(a, b);
	ab = ab * ratio;

	//float2 xy = float2(ab.x, ab.y);
	float2 xy = float2(ab.x + sign(ab.x) * .5, ab.y + sign(ab.y) * .5);
	float delta = 0.01;

	for (int i = 0; i < 100; i++)
	{
		float f1 = F1(xy, ab.xy, distortion);
		float f2 = F2(xy, ab.xy, distortion);
		float2x2 J = Jacobian2x2(xy, ab, delta, distortion);
		float2x2 Ji = Inverse2x2(J);

		float2 xyNew = xy - mul(Ji, float2(f1, f2));
		float2 diff = xyNew - xy;

		if (length(diff) < 0.001)
		{
			xy = xyNew;
			break;
		}
		xy = xyNew;
	}

	float u = (outerx + xy.x) / (2 * outerx);
	float v = 1 - (outery + xy.y) / (2 * outery);

	return float2(u, v);
}