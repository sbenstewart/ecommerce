import { Component } from '@angular/core';
import { NavController, NavParams, ToastController } from 'ionic-angular';

import * as WC from 'woocommerce-api';
import { ProductDetailsPage }  from '../product-details/product-details';

@Component({
  selector: 'page-products-by-category',
  templateUrl: 'products-by-category.html',
})
export class ProductsByCategoryPage {

	WooCommerce: any;
	products: any;
	page: number;
	category: any;

  constructor(
    public navCtrl: NavController, 
    public navParams: NavParams,
    public toastCtrl: ToastController
  ) {

  	this.page = 1;
  	this.category = this.navParams.get('category');

  	this.WooCommerce = WC({
  		url: 						"http://edwintrivinos.com/cusstom_apps/woo_commerce_ionic_3/",
  		consumerKey: 		"ck_8f07070deceadfc7cbaffdb425b5621ef91e9956",
  		consumerSecret: "cs_732111e679d2bd8563bec6c03d670a56680a26d2"
  	});

  	this.WooCommerce.getAsync("products?filter[category]=" + this.category.slug)
	  	.then((data) => {
        this.products = JSON.parse(data.body).products;
	  	}, (error) => {
	  		console.log(error);
	  	});
  }

  ionViewDidLoad() {
    console.log('ionViewDidLoad ProductsByCategoryPage');
  }

  loadMoreProducts(event) {
    console.log('loadMoreProducts');
    //this.page++;

    this.WooCommerce.getAsync("products?filter[category]=" + this.category.slug + '&page=' + this.page)
      .then((data) => {
        let moreProducts = JSON.parse(data.body).products;
        this.products = this.products.concat(moreProducts);

        event.complete(); 

        if(moreProducts.length < 10) { 
          event.enable(false); 

          this.toastCtrl.create({
            message: 'No more products!',
            duration: 1000
          }).present();
        }
      }, (error) => {
        console.log(error);
      });
  }

  openProductPage(product) {
    this.navCtrl.push(ProductDetailsPage, { product: product });
  }

}
 