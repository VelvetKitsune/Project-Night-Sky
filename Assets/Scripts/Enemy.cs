using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour {
	
	
	//public Animator animator;
	
	// General Stats
	public Level level;
	
	public int maxHealth;
	private int currentHealth;
	
	public float speed;
	
	public int experienceOnDeath;
	
	private float dazedTime;
	public float startDazedTime;
	
	public HealthBar healthBar;
	
	void Start() {
		currentHealth = maxHealth;
	}
	
	void Update() {
		if (dazedTime <= 0) {
			speed = 4;
		} else {
			speed = 0;
			dazedTime -= Time.deltaTime;
		}
		
		
	}
	
	public void TakeDamage (int damage) {
		dazedTime = startDazedTime;
		currentHealth -= damage;
		healthBar.SetHealth (currentHealth);
		
		//animator.SetTrigger("Hurt");
		
		if (currentHealth <= 0) {
			Die();
		}
	}
	
	void Die() {
		Debug.Log ("Enemy ded.");
		
		GameObject.Find("Player").GetComponent<Player>().level.AddExp(experienceOnDeath);
		GameObject.Find("Player").GetComponent<Player>().expBar.slider.value = GameObject.Find("Player").GetComponent<Player>().level.experience;
		
		//animator.SetBool("IsDead", true);
		
		GetComponent<Collider2D>().enabled = false;
		this.enabled = false;
	}
	
}
