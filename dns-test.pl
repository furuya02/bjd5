for ($i=0;;$i++) {
	# $cmd = "nslookup $i.google.com 127.0.0.1\n";
	$cmd = "nslookup -timeout=0 www.example.com 127.0.0.1\n";
	if($i>3000){
		$i=0;
	}
	`$cmd`;
}