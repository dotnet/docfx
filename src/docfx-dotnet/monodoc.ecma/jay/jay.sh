#!/bin/sh

TOPDIR=$1
INPUT=$2
OUTPUT=$3
FLAGS=$4

TEMPFILE=`mktemp jay-tmp.XXXXXX` || exit 1

$TOPDIR/jay/jay $FLAGS < $TOPDIR/jay/skeleton $INPUT > $TEMPFILE && mv $TEMPFILE $OUTPUT
