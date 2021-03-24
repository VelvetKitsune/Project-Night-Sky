using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ManaBar : MonoBehaviour {
	
	public Slider slider;
	public Gradient gradient;
	public Image fill;
	public Text manaCount;
	
	public void SetMaxMana(int mana) {
		slider.maxValue = mana;
		slider.value = mana;
		
		fill.color = gradient.Evaluate(1f);
	}
	
	public void SetMana(int mana) {
		slider.value = mana;
		
		fill.color = gradient.Evaluate(slider.normalizedValue);
		manaCount.text = (slider.value) + ("/") + (slider.maxValue).ToString();
	}


}
