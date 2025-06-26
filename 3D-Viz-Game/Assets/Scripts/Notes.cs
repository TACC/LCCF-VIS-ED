        //logic as to how to spawn different UI elements on different roles
        // // note to self - this logic will be used for cashier making a ticket and handing it over to kitchen
        // GameObject buttonObj = GameObject.Find("ScoreButton");
        // GameObject score = GameObject.Find("ScoreText");

        // if (buttonObj != null && score != null)
        // {
        //     scoreButton = buttonObj.GetComponent<Button>();
        //     scoreText = score.GetComponent<Text>();


        //     scoreButton.gameObject.SetActive(false); // everyone gets a button
        //     scoreText.gameObject.SetActive(false);


        //     // if (assignedRole == "Cashier")
        //     // {
        //     //     Debug.Log("After checking assigned role");
        //     //     // only enabling score on the cashier's screen

        //     //     scoreText.gameObject.SetActive(true);
        //     //     //scoreButton.gameObject.SetActive(false);
        //     // }
        // }

        // connecting client to matching player model


           // this is to add picking stuff up but won't need since plan
    // is to just have fake animations
    // void OnTriggerEnter(Collider c)
    // {
    //     if (!IsServerInitialized) return;

    //     if (c.gameObject.CompareTag("Pickup"))
    //     {
    //         c.gameObject.SetActive(false);
    //         count += 1;
    //         UpdateCountTextRpc(count);
    //     }
    // }


// used when I had a point system
        // [ObserversRpc]
    // private void UpdateCountTextRpc(int newCount)
    // {
    //     if (countText != null)
    //         countText.text = "Count: " + newCount.ToString();
    // }