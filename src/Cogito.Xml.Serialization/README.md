Every global element gets an implicit type. If the element specifies a conmplex type, then it inherits from it.
Every global type gets a type.

When generating a type, each element that specifies it's own type content gets it's own type,
 nested under that type. Same logic: it might inherit from another.

