using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent (typeof (Controller2D))]
public class Player : MonoBehaviour {
	
	public Animator animator;
	
	// Character Stats
	public Level level;
	public int currLevel = 1;
	public Text levelCount;
	
	public ExpBar expBar;
	
	public int maxHealth = 250;
	public int currentHealth;
	public HealthBar healthBar;
	
	public int maxMana = 100;
	public int currentMana;
	public ManaBar manaBar;
	
	public int strengthStat;
	public int agilityStat;
	public int skillStat;
	public int ferocityStat;
	public int intelligenceStat;
	
	// Movement variables!
	public float maxJumpHeight = 5; 
	public float minJumpHeight = 2;
	public float timeToJumpApex = .4f; //Essentially jump speed
	float accelerationTimeAirborne = .2f;
	float accelerationTimeGrounded = .1f;
	public float moveSpeed = 6;
	
	// Extra abilities!
	public int extraJumps = 0; //Number of jumps added ON TO the character, so 1 here means a double jump.
	int jumpCount = 0; //Tracks how many jumps the character has done real time. Just leave at 0!
	
	public bool hasDash = false;
	public float DashForce;
	public float StartDashTimer;
	public float distanceBetweenImages;
	public float dashCooldown = 1;
	
	float CurrentDashTimer;
	float DashDirection;
	
	private float lastDash = -100f;
	private float lastImageXpos;
	
	bool isDashing = false;
	// End of extra abilities.
	
	// Attack Stuff
	public int damage;
	
	public float attackRate = 1.5f;
	float nextAttackTime = 0f;
	
	public Transform attackPoint;
	public float attackRange = 1.76f;
	public LayerMask enemyLayers;
	
	// End Attack Stuff
	
	public Vector2 wallJumpClimb;
	public Vector2 wallJumpOff;
	public Vector2 wallLeap;

	
	public float wallSlideSpeedMax = 3;
	public float wallStickTime = .15f;
	float timeToWallUnstick;
	
	float gravity;
	float maxJumpVelocity;
	float minJumpVelocity;
	Vector3 velocity;
	float velocityXSmoothing;
	
	Controller2D controller; 
	
	Vector2 directionalInput;
	bool wallSliding;
	int wallDirX;
	
	private Rigidbody2D rb;
	private bool facingRight = true;
	// End Movement Variables

    void Start() {
		controller = GetComponent<Controller2D> ();
		
		rb = GetComponent<Rigidbody2D>();
		
		gravity = -(2 * maxJumpHeight) / Mathf.Pow (timeToJumpApex, 2);
		maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
		minJumpVelocity = Mathf.Sqrt(2*Mathf.Abs (gravity) * minJumpHeight);
		
		currentHealth = maxHealth;
		healthBar.SetMaxHealth (maxHealth);
		healthBar.healthCount.text = (currentHealth) + ("/") + (maxHealth).ToString();
		
		currentMana = maxMana;
		manaBar.SetMaxMana (maxMana);
		manaBar.manaCount.text = (currentMana) + ("/") + (maxMana).ToString();
		
		level = new Level(1, OnLevelUp);
		
		expBar.slider.minValue = level.GetXPForLevel (level.currentLevel);
		expBar.slider.maxValue = level.GetXPForLevel (level.currentLevel + 1);
    }
	
	private void FixedUpdate() {
		if (velocity.y == 0) {
			animator.SetBool("IsJumping", false);
			animator.SetBool("IsFalling", false);
		}
		if (velocity.y != 0) {
			animator.SetBool("IsFalling", true);
		}
	}
	
	void Update() {
		// Dummy HP drain test
		if (Input.GetKeyDown (KeyCode.Space)) {
			TakeDamage(25);
			ManaDown(25);
		}
		// Dummy HP heal test
		if (Input.GetKeyDown (KeyCode.Return)) {
			HealDamage(15);
			ManaUp(15);
		}
		// Dummy Level Up test
		if (Input.GetKeyDown (KeyCode.M)) {
			level.AddExp(100);
			expBar.slider.value = level.experience;
		}
		
		//Controls and stuff
		// Jump stuff
		Vector2 directionalInput = new Vector2 (Input.GetAxisRaw ("Horizontal"), Input.GetAxisRaw ("Vertical"));
		SetDirectionalInput (directionalInput);
		
		if (Input.GetKeyDown (KeyCode.X)) {
			OnJumpInputDown ();
		}
		if (Input.GetKeyUp (KeyCode.X)) {
			OnJumpInputUp ();
		}
		// Dash stuff
		if (hasDash == true) {
			if (Input.GetKeyDown (KeyCode.C)) {
				if (Time.time >= (lastDash + dashCooldown)) {
					OnDashInput ();
				}
			}
		}
		
		// Hit enemies with large weapon button!
		if (Time.time >= nextAttackTime) {
			if (Input.GetKeyDown (KeyCode.Z)) {
				Attack();
				nextAttackTime = Time.time + 1f / attackRate;
			}
		}
		//End player controls
		
		CalculateVelocity ();
		HandleWallSliding ();
		if (isDashing == true) {
			animator.SetBool("IsDashing", true);
			CheckDash();
		} else {
			animator.SetBool("IsDashing", false);
		}
		
		//The flippening (Flips sprites when you turn, of course!)
		if (controller.collisions.below) {
			if (facingRight == false && (int)directionalInput.x > 0) {
				Flip();
			} else if(facingRight == true && (int)directionalInput.x < 0) {
				Flip();
			}
		}
		
		controller.Move (velocity * Time.deltaTime, directionalInput);
		
		if (controller.collisions.above || controller.collisions.below) {
			if (controller.collisions.slidingDownMaxSlope) {
				velocity.y += controller.collisions.slopeNormal.y * -gravity * Time.deltaTime;
			} else {
				velocity.y = 0;
			}
		}
	}
	
	void Flip() {
		facingRight = !facingRight;
		Vector3 Scaler = transform.localScale;
		Scaler.x *= -1;
		transform.localScale = Scaler;
	}
	
	public void SetDirectionalInput (Vector2 input) {
		directionalInput = input;
	}
	
	//Full jump stuff
	public void OnJumpInputDown() {
		if (wallSliding) {
			if (wallDirX == directionalInput.x) {
				velocity.x = -wallDirX * wallJumpClimb.x;
				velocity.y = wallJumpClimb.y;
			}
			else if (directionalInput.x == 0) {
				velocity.x = -wallDirX * wallJumpOff.x;
				velocity.y = wallJumpOff.y;
			}
			else {
				velocity.x = -wallDirX * wallLeap.x;
				velocity.y = wallLeap.y;
			}
		}
		if (controller.collisions.below) {
			jumpCount = 0;
			if (controller.collisions.slidingDownMaxSlope) {
				if (directionalInput.x != -Mathf.Sign(controller.collisions.slopeNormal.x)) { // Not jumping against max slope
					velocity.y = maxJumpVelocity * controller.collisions.slopeNormal.y;
					velocity.x = maxJumpVelocity * controller.collisions.slopeNormal.x;
				}
			} else {
				//Single full jump
				velocity.y = maxJumpVelocity;
				animator.SetBool("IsJumping", true);
			}
		} else if (jumpCount < extraJumps) {
			//Multi jumps
			velocity.y = maxJumpVelocity;
			jumpCount++;
			animator.SetBool("IsJumping", true);
		}
	}
	
	//Short jump stuff
	public void OnJumpInputUp() {
		if (velocity.y > minJumpVelocity) {
			velocity.y = minJumpVelocity;
		}
	}
	
	public void OnLanding() {
		animator.SetBool("IsJumping", false);
	}
	
	void HandleWallSliding() {
		wallDirX = (controller.collisions.left) ? -1 : 1;
		wallSliding = false;
		if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y <0) {
			wallSliding = true;
			
			if (velocity.y < -wallSlideSpeedMax) {
				velocity.y = -wallSlideSpeedMax;
			}
			
			if (timeToWallUnstick > 0) {
				velocityXSmoothing = 0;
				velocity.x = 0;
				
				if (directionalInput.x != wallDirX && directionalInput.x != 0) {
					timeToWallUnstick -= Time.deltaTime;
				}
				else {
					timeToWallUnstick = wallStickTime;
				}
			}
			else {
				timeToWallUnstick = wallStickTime;
			}
		}
	}
	
	//Dash action!
	private void OnDashInput() {
		if (directionalInput.x != 0) {
			isDashing = true;
			CurrentDashTimer = StartDashTimer;
			velocity = Vector2.zero;
			DashDirection = (int)directionalInput.x;
			lastDash = Time.time;
			
			PlayerAfterImagePool.Instance.GetFromPool();
	//		lastImageXpos = transform.position.x;
		}
	}
	
	private void CheckDash() {
		velocity = transform.right * DashDirection * DashForce;
		
		CurrentDashTimer -= Time.deltaTime;
	//	if(Mathf.Abs(transform.position.x - lastImageXpos) > distanceBetweenImages) {
	//		PlayerAfterImagePool.Instance.GetFromPool();
	//		lastImageXpos = transform.position.x;
	//	}
		if(CurrentDashTimer <= 0) {
			isDashing = false;
		}
	}
	
	void CalculateVelocity() {
		float targetVelocityX = directionalInput.x * moveSpeed;
		velocity.x = Mathf.SmoothDamp (velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below)?accelerationTimeGrounded:accelerationTimeAirborne);
		velocity.y += gravity * Time.deltaTime;
		animator.SetFloat("Speed", Mathf.Abs(velocity.x));
	}
	
	//Collision tracker! (Like for items or running into a hurty thing)
	//private void OnTriggerEnter2D(Collider2D collider) {
	//	if (collider.CompareTag("Collectable")) {
	//		
	//		//Rudimentary inventory system, tells you what you've just picked up.
	//		string itemType = collider.gameObject.GetComponent<Collectables>().itemType;
	//		print("Yoinked a " + itemType);
	//		
	//		//If you get the "double-jump" item, it adds 1 to extra jumps, simple!
	//		if (itemType == "double-jump") {
	//			extraJumps = extraJumps+1;
	//		}
	//		
	//		//If you get the "dash" item, it adds the ability to dash, neat!
	//		if (itemType == "dash") {
	//			hasDash = true;
	//		}
	//		
	//		//This adds item to your inventory... But there's not an actual inventory right now.
	//		inventory.Add(itemType);
	//		print("Inventory length: " + inventory.Count);
	//		
	//		//Destroys the item you've just picked up so you can't repeatedly grab it.
	//		Destroy(collider.gameObject);
	//	}
	//}
	
	//Level stuff!
	public void OnLevelUp() {
		print ("Level up!");
		if (maxHealth < 2000) {
		maxHealth = Mathf.RoundToInt(maxHealth * 1.08f);
		maxMana = Mathf.RoundToInt(maxMana * 1.08f);
		} else if (maxHealth < 4000) {
		maxHealth = Mathf.RoundToInt(maxHealth * 1.04f);
		maxMana = Mathf.RoundToInt(maxMana * 1.04f);
		} else {
		maxHealth = Mathf.RoundToInt(maxHealth * 1.02f);
		maxMana = Mathf.RoundToInt(maxMana * 1.02f);
		}
		
		currentHealth = maxHealth;
		healthBar.SetMaxHealth (maxHealth);
		healthBar.healthCount.text = (currentHealth) + ("/") + (maxHealth).ToString();
		
		currentMana = maxMana;
		manaBar.SetMaxMana (maxMana);
		manaBar.manaCount.text = (currentMana) + ("/") + (maxMana).ToString();
		
		currLevel += 1;
		levelCount.text = (currLevel).ToString();
		
		expBar.slider.minValue = level.GetXPForLevel (level.currentLevel);
		expBar.slider.maxValue = level.GetXPForLevel (level.currentLevel + 1);
	}
	
	// HP Stuff
	//Damage Taken
	public void TakeDamage (int damageAmount) {
		currentHealth -= damageAmount;
		
		if (currentHealth < 0) {
			currentHealth = 0;
		}
		
		healthBar.SetHealth (currentHealth);
		healthBar.healthCount.text = (currentHealth) + ("/") + (maxHealth).ToString();
	}
	
	//Damage Healed
	public void HealDamage (int healAmount) {
		currentHealth += healAmount;
		
		if (currentHealth > maxHealth) {
			currentHealth = maxHealth;
		}
		
		healthBar.SetHealth (currentHealth);
		healthBar.healthCount.text = (currentHealth) + ("/") + (maxHealth).ToString();
	}
	
	// MP Stuff
	//Mana goes down
	public void ManaDown (int manaDownAmount) {
		currentMana -= manaDownAmount;
		
		if (currentMana < 0) {
			currentMana = 0;
		}
		
		manaBar.SetMana (currentMana);
		manaBar.manaCount.text = (currentMana) + ("/") + (maxMana).ToString();
	}
	
	//Mana goes up!
	public void ManaUp (int manaUpAmount) {
		currentMana += manaUpAmount;
		
		if (currentMana > maxMana) {
			currentMana = maxMana;
		}
		
		manaBar.SetMana (currentMana);
		manaBar.manaCount.text = (currentMana) + ("/") + (maxMana).ToString();
	}
	
	//Attack Function.
	void Attack() {
		animator.SetTrigger("Attack");
		
		Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
		
		foreach (Collider2D enemy in hitEnemies) {
			enemy.GetComponent<Enemy>().TakeDamage(damage);
		}
	}
	
	//Visualise hitbox
	void OnDrawGizmosSelected() {
		if (attackPoint == null)
			return;
		
		Gizmos.DrawWireSphere(attackPoint.position, attackRange);
	}
}