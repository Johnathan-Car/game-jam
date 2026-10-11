using Godot;
using System;

public partial class HealthBar : HSlider
{
	public void SetHealth(int h)
	{
		Value = h;
		StyleBoxFlat fill = GetThemeStylebox("grabber_area") as StyleBoxFlat;
		Vector3 temp = new Vector3(1, 0, 0).Lerp(new Vector3(0, 1, 0), (float)(Value / MaxValue));
		fill.BgColor = new Color(temp.X, temp.Y, temp.Z, 1.0f);
	}

	public void SetMaxHealth(int m)
	{
		MaxValue = m;
	}
}
