window.cart = {
    getCartQuantities: function() {
        var cartStr = localStorage.getItem('cart');
        if (!cartStr) return {};
        try {
            return JSON.parse(cartStr) || {};
        } catch (e) {
            return {};
        }
    },

    addItem: function(stripeProductID, name) {
        var cart = this.getCartQuantities();
        cart[stripeProductID] = (cart[stripeProductID] || 0) + 1;
        localStorage.setItem('cart', JSON.stringify(cart));
        this.showToast(name + " has been added to your cart.", "success");
    },

    removeItem: function(stripeProductID, name) {
        var cart = this.getCartQuantities();
        if (cart[stripeProductID]) {
            if (cart[stripeProductID] > 1) {
                cart[stripeProductID] -= 1;
                localStorage.setItem('cart', JSON.stringify(cart));
                this.showToast(name + " quantity has been decreased.", "success");
            } else {
                delete cart[stripeProductID];
                localStorage.setItem('cart', JSON.stringify(cart));
                this.showToast(name + " has been removed from your cart.", "success");
            }
        }
    },

    removeItemAndRefresh: function(stripeProductID, name) {
        var cart = this.getCartQuantities();
        if (cart[stripeProductID]) {
            if (cart[stripeProductID] > 1) {
                cart[stripeProductID] -= 1;
                localStorage.setItem('cart', JSON.stringify(cart));
                this.showToast("A " + name + " order has been removed from your cart.", "success");
            } else {
                delete cart[stripeProductID];
                localStorage.setItem('cart', JSON.stringify(cart));
                this.showToast(name + " has been removed from your cart.", "success");
            }
            this.renderCartPage();
        }
    },

    removeItemFromPage: function(stripeProductID, name) {
        var cart = this.getCartQuantities();
        if (cart[stripeProductID]) {
            delete cart[stripeProductID];
            localStorage.setItem('cart', JSON.stringify(cart));
            this.showToast(name + " has been removed from your cart.", "success");
            this.renderCartPage();
        }
    },

    clearItems: function() {
        localStorage.removeItem('cart');
    },

    showToast: function(message, level) {
        var container = document.getElementById('toast-container');
        var template = document.getElementById('toast-template');
        if (!container || !template) return;

        var bgClass = '';
        if (level === 'success') bgClass = 'bg-success';
        else if (level === 'warning') bgClass = 'bg-warning';
        else if (level === 'error') bgClass = 'bg-danger';

        var clone = template.content.cloneNode(true);
        var toastDiv = clone.querySelector('.toast');
        var toastBody = clone.querySelector('.toast-body');
        var closeBtn = clone.querySelector('.btn-close');

        var toastId = 'toast-' + Date.now() + '-' + Math.floor(Math.random() * 1000);
        if (toastDiv) {
            toastDiv.id = toastId;
            if (bgClass) {
                toastDiv.classList.add(bgClass);
            }
        }

        if (toastBody) {
            toastBody.textContent = message; // Safely escape string values
        }

        if (closeBtn) {
            closeBtn.onclick = function() {
                var el = document.getElementById(toastId);
                if (el) el.remove();
            };
        }

        container.appendChild(clone);

        setTimeout(function() {
            var el = document.getElementById(toastId);
            if (el) el.remove();
        }, 4000);
    },

    renderCartPage: function() {
        var loadingEl = document.getElementById('cart-loading');
        var emptyEl = document.getElementById('empty-cart');
        var contentEl = document.getElementById('cart-content-wrapper');
        var itemsListEl = document.getElementById('cart-items-list');
        var subtotalEl = document.getElementById('cart-subtotal');
        var totalEl = document.getElementById('cart-total');
        var template = document.getElementById('cart-item-template');

        if (!loadingEl || !emptyEl || !contentEl || !itemsListEl || !template) return;

        loadingEl.style.display = 'block';
        emptyEl.style.display = 'none';
        contentEl.style.display = 'none';
        itemsListEl.innerText = '';

        var cart = this.getCartQuantities();
        var productIDs = Object.keys(cart);

        if (productIDs.length === 0) {
            loadingEl.style.display = 'none';
            emptyEl.style.display = 'block';
            return;
        }

        fetch('/api/products/details', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(productIDs)
        })
        .then(function(res) {
            if (!res.ok) throw new Error('Failed to load product details');
            return res.json();
        })
        .then(function(products) {
            if (!products || products.length === 0) {
                loadingEl.style.display = 'none';
                emptyEl.style.display = 'block';
                return;
            }

            var subtotal = 0;
            var fragment = document.createDocumentFragment();

            products.forEach(function(product) {
                var stripeProductID = product.stripeProductID || product.StripeProductID;
                var qty = cart[stripeProductID] || 0;
                if (qty <= 0) return;

                var price = product.price || product.Price;
                var name = product.name || product.Name;
                var description = product.description || product.Description;

                var itemTotal = price * qty;
                subtotal += itemTotal;

                var clone = template.content.cloneNode(true);
                var cardDiv = clone.querySelector('.cart-item-card');
                var titleEl = clone.querySelector('.cart-item-title');
                var descEl = clone.querySelector('.cart-item-desc');
                var qtyEl = clone.querySelector('.qty-val');
                var priceSingleEl = clone.querySelector('.price-single');
                var priceTotalEl = clone.querySelector('.price-total');
                var removeBtn = clone.querySelector('.btn-remove');

                if (cardDiv) {
                    cardDiv.id = 'cart-item-' + stripeProductID;
                }

                if (titleEl) titleEl.textContent = name;
                if (descEl) descEl.textContent = description;
                if (qtyEl) qtyEl.textContent = qty;
                if (priceSingleEl) priceSingleEl.textContent = '$' + price.toFixed(2) + ' ea';
                if (priceTotalEl) priceTotalEl.textContent = '$' + itemTotal.toFixed(2);

                if (removeBtn) {
                    removeBtn.onclick = function() {
                        window.cart.removeItemAndRefresh(stripeProductID, name);
                    };
                }

                fragment.appendChild(clone);
            });

            if (subtotal === 0) {
                loadingEl.style.display = 'none';
                emptyEl.style.display = 'block';
                return;
            }

            itemsListEl.appendChild(fragment);
            subtotalEl.textContent = '$' + subtotal.toFixed(2);
            totalEl.textContent = '$' + subtotal.toFixed(2);

            loadingEl.style.display = 'none';
            contentEl.style.display = 'flex';
        })
        .catch(function(err) {
            console.error(err);
            loadingEl.style.display = 'none';
            emptyEl.style.display = 'block';
            window.cart.showToast('Error loading cart items.', 'error');
        });
    },

    checkout: function() {
        var quantities = this.getCartQuantities();
        if (Object.keys(quantities).length === 0) {
            this.showToast("Your cart is empty!", "error");
            return;
        }

        var btn = document.getElementById('checkout-btn');
        var originalText = '';
        if (btn) {
            btn.disabled = true;
            originalText = btn.innerText;
            btn.innerText = 'Connecting to Stripe...';
        }

        fetch('/api/products/checkout', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(quantities)
        })
        .then(function(res) {
            if (!res.ok) throw new Error('Checkout session creation failed');
            return res.json();
        })
        .then(function(data) {
            if (data.url) {
                window.location.href = data.url;
            } else {
                throw new Error('Checkout URL not returned');
            }
        })
        .catch(function(err) {
            console.error(err);
            window.cart.showToast('Checkout failed. Please try again.', 'error');
            if (btn) {
                btn.disabled = false;
                btn.innerText = originalText;
            }
        });
    },

    initPage: function() {
        if (document.getElementById('cart-loading')) {
            window.cart.renderCartPage();
        }
    }
};

// Auto-trigger rendering based on browser lifecycles and Blazor dynamic loading events
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', window.cart.initPage);
} else {
    window.cart.initPage();
}

// Hook into Blazor's static SSR enhanced page navigation events
if (typeof Blazor !== 'undefined' && Blazor.addEventListener) {
    Blazor.addEventListener('enhancedload', window.cart.initPage);
} else {
    document.addEventListener('DOMContentLoaded', function() {
        if (typeof Blazor !== 'undefined' && Blazor.addEventListener) {
            Blazor.addEventListener('enhancedload', window.cart.initPage);
        }
    });
}
