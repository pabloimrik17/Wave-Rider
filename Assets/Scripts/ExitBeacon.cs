﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitBeacon : MonoBehaviour {
	// Use this for initialization
	void Start () {
		//sound = GetComponent<AudioSource> ();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void OnTriggerEnter2D (Collider2D collider) {
		if (collider.tag == "NutShip") {
			//sound.Play ();
			AudioManager.Instance.playSound("Win.wav"); //TODO TEMPORALMENTE ROTO, PUES NO ESPERA QUE SE ACABE DE REPRODUCIR EL SONIDO
			GameManager.Instance.winLevel ();
			Rigidbody2D nutRigi = collider.GetComponent<Rigidbody2D> ();
			nutRigi.constraints = RigidbodyConstraints2D.FreezeAll;
		}
	}
}
