{
 var outer_x = 0;
 var inner_x = 0;
 var sum = 0;
 var i = 0;
 var a = 0;

 #1 = 5;
 r1 = #1;
 result1 = r1 * 2 + pow(3,2);
 sin30 = sin(30);
 pi_val = pi();
 bit = ~1;
 shift = 2 << 3;
 cmp = (2 + 3 == 5);
 logic = (1 && 0) || (1 && 1);
 atan = atan2(1,1);

 { var x = 10; { var x = 20; inner_x = x; } outer_x = x; }

 for (i = 0; i < 5; i = i + 1) { sum = sum + i; }

 a = 1; while (a < 4) { a = a + 1; }

 #(2,4,8) = #5 + 1;
 val_rd = #(2,4,8);

 final = result1 + sin30 + pi_val + shift + outer_x + inner_x + sum + a + val_rd;
 final
}
