# Commerce Payment Providers
Open-sourced payment gateways for Episerver Commerce. This solution contains:
* Epay payment provider project.
This provider connects Episerver Commerce with DIBS, a popular and widely used system for accepting credit card payments.
* Test project

## Project structure
The structure of Epat follows the offical from Episerver on DataCash, DIBS and PayPal projects is similar, each project contains:
* CommerceManager folder: contains  files to be deployed to the Commerce Manager site.
* Controllers folder: contains controllers used for redirecting payment.
* Frontend folder: contains files that need to be deployed to the front-end site when install. We support both webform site and MVC site.
Note that the MVC view files (.cshtml files) are based on the MVC sample site Quicksilver (please refer to https://github.com/episerver/Quicksilver for more detail),
you might need to custom those or create new views for your site.
* Helper folder: contains some helper classes.
* lang folder: contains language files.
* Lib folder (if any): contains referenced DLLs if any.
* PageTypes folder: contains a file that defines a Episerver CMS page type for the payment.
* Other files: define payment, payment gateway, payment option classes, payment meta class.
